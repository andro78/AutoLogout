const vm = require('node:vm');
const fs = require('node:fs');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../extension/background.js'), 'utf8');
async function main() {
  let calls = 0, fail = true, state = {}, messages = [], listener;
  const port = { postMessage: x => messages.push(x), onMessage: {addListener: f => listener=f}, onDisconnect: {addListener() {}} };
  const context = vm.createContext({ URL, Date, Promise, console, setTimeout() {}, crypto: {randomUUID: () => 'profile-test'}, navigator: {userAgent: 'Edg/123'}, chrome: {
    storage: {local: {get: async () => ({...state}), set: async x => Object.assign(state,x)}},
    browsingData: {remove: async () => { calls++; if(fail) throw Error('simulated failure'); }},
    runtime: {connectNative: () => port, onStartup: {addListener() {}}},
    alarms: {create() {}, onAlarm: {addListener() {}}}
  }});
  vm.runInContext(source, context);
  const msg = {type:'state',settings:{Origins:['https://example.com'],OnStartup:false},signal:{Id:'one'}};
  context.msg=msg;
  await vm.runInContext('handle(msg)',context);
  assert.equal(state.lastDoneId,undefined);
  fail=false;
  await vm.runInContext('handle(msg)',context);
  assert.equal(state.lastDoneId,'one');
  await vm.runInContext('handle(msg)',context);
  assert.equal(calls,2,'successful request must not repeat');
  msg.signal.Id='two';msg.settings.Origins=['http://example.com'];
  await vm.runInContext('handle(msg)',context);
  assert.equal(calls,2,'invalid origin must not delete data');
  assert.equal(state.lastDoneId,'one');
  msg.settings.Origins=[];
  await vm.runInContext('handle(msg)',context);
  assert.equal(state.lastDoneId,'two');
  assert(messages.some(x=>x.type==='result' && x.browser==='edge'));
  console.log('PASS: failed cleanup retry, success deduplication, invalid origin rejection, empty allowlist, Edge result reporting');
}
main().catch(e=>{console.error(e);process.exitCode=1});
