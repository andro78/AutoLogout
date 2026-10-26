const vm = require('node:vm');
const fs = require('node:fs');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../extension/background.js'), 'utf8');
async function main() {
  let calls = 0, fail = true, cookieFail = false, state = {}, messages = [], listener;
  const port = { postMessage: x => messages.push(x), onMessage: {addListener: f => listener=f}, onDisconnect: {addListener() {}} };
  const context = vm.createContext({ URL, Date, Promise, console, setTimeout() {}, crypto: {randomUUID: () => 'profile-test'}, navigator: {userAgent: 'Edg/123'}, chrome: {
    storage: {local: {get: async () => ({...state}), set: async x => Object.assign(state,x)}},
    browsingData: {remove: async (options, types) => { assert.equal(types.cookies, undefined); calls++; if(fail) throw Error('simulated failure'); }, removeCookies: async () => { if(cookieFail) throw Error('cookie failure'); }},
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
  msg.signal.Id='three';msg.settings.Origins=['https://www.google.com/'];cookieFail=true;
  await vm.runInContext('handle(msg)',context);
  assert.equal(state.lastDoneId,'two','cookie failure must remain pending');
  assert.equal(state.lastResult.ok,false);
  cookieFail=false;
  await vm.runInContext('handle(msg)',context);
  assert.equal(state.lastDoneId,'three');
  await vm.runInContext("send('heartbeat')",context);
  assert.equal(messages.at(-1).result.id,'three','heartbeat must retain result for app polling');
  assert.equal(messages.at(-1).result.ok,true);
  const closeId='0123456789abcdef0123456789abcdef';
  const marker='about:blank#autologout-'+closeId;
  const closedWindows=[],closedTabs=[];
  context.chrome.windows={getAll:async()=>[
    {id:1,tabs:[{id:11,url:marker}]},
    {id:2,tabs:[{id:21,url:marker},{id:22,url:'https://example.com'}]},
    {id:3,tabs:[{id:31,url:'https://example.com'}]}
  ],remove:async id=>closedWindows.push(id)};
  context.chrome.tabs={remove:async id=>closedTabs.push(id)};
  context.closeId=closeId;
  await vm.runInContext('closeCleanupWindows(closeId)',context);
  assert.deepEqual(closedWindows,[1]);
  assert.deepEqual(closedTabs,[21],'user tabs and windows must stay open');
  closedWindows.length=0;closedTabs.length=0;
  msg.signal.Id=closeId;cookieFail=true;
  await vm.runInContext('handle(msg)',context);
  assert.equal(closedWindows.length,0,'failed cleanup must not close browser');
  cookieFail=false;
  await vm.runInContext('handle(msg)',context);
  assert.deepEqual(closedWindows,[1],'successful cleanup closes owned window');
  console.log('PASS: failed cleanup retry, success deduplication, invalid origin rejection, empty allowlist, Edge result reporting');
}
main().catch(e=>{console.error(e);process.exitCode=1});
