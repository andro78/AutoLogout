const HOST = 'com.autologout.bridge';
let port = null;
let processing = Promise.resolve();
const browser = /Edg\//.test(navigator.userAgent) ? 'edge' : 'chrome';
async function send(type, extra = {}) {
  if (!port) return;
  const data = await chrome.storage.local.get('profileId');
  const profile = data.profileId || crypto.randomUUID();
  if (!data.profileId) await chrome.storage.local.set({ profileId: profile });
  try { port.postMessage({ type, browser, profile, ...extra }); } catch {}
}
async function handle(msg) {
  if (msg.type !== 'state') return;
  const id = msg.signal?.Id;
  const data = await chrome.storage.local.get(['lastDoneId', 'startupComplete']);
  const startup = msg.settings?.OnStartup !== false && !data.startupComplete;
  if ((!id || id === data.lastDoneId) && !startup) return;
  const requestId = id || 'browser-' + Date.now();
  const result = { id: requestId, time: new Date().toISOString(),
    reason: msg.signal?.Reason || 'browser-startup', ok: false };
  try {
    if (!Array.isArray(msg.settings?.Origins)) throw new Error('앱에서 설정을 저장하세요.');
    const origins = [...new Set(msg.settings.Origins.map(value => {
      const url = new URL(value);
      if (url.protocol !== 'https:' || url.username || url.password ||
          url.pathname !== '/' || url.search || url.hash) throw new Error('Invalid HTTPS origin');
      return url.origin;
    }))];
    result.origins = origins;
    if (origins.length) await chrome.browsingData.remove({ origins }, {
      cookies: true, localStorage: true, indexedDB: true, cacheStorage: true, serviceWorkers: true
    });
    result.ok = true;
    await chrome.storage.local.set({ lastDoneId: requestId, startupComplete: true });
  } catch (error) { result.error = String(error); }
  await chrome.storage.local.set({ lastResult: result });
  await send('result', { result });
}
function connect() {
  if (port) return;
  try {
    const active = chrome.runtime.connectNative(HOST);
    port = active;
    active.onMessage.addListener(msg => {
      processing = processing.then(async () => {
        await chrome.storage.local.set({ connection: { connected: true, time: new Date().toISOString() } });
        await send('heartbeat');
        await handle(msg);
      }).catch(console.error);
    });
    active.onDisconnect.addListener(() => {
      const error = chrome.runtime.lastError?.message || 'Disconnected';
      if (port === active) port = null;
      chrome.storage.local.set({ connection: { connected: false, error, time: new Date().toISOString() } });
      setTimeout(connect, 5000);
    });
    send('heartbeat');
  } catch (error) { console.error(error); }
}
chrome.alarms.create('retry', { periodInMinutes: 1 });
chrome.alarms.onAlarm.addListener(() => { if (!port) connect(); else send('heartbeat'); });
chrome.runtime.onStartup.addListener(() => {
  processing = processing.then(() => chrome.storage.local.set({ startupComplete: false })).then(connect);
});
connect();

