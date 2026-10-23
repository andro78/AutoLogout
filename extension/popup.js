chrome.storage.local.get(['lastResult', 'connection']).then(data => {
 document.getElementById('status').textContent =
  JSON.stringify({ connection: data.connection || '연결 확인 대기', lastResult: data.lastResult || '실행 결과 없음' }, null, 2);
});

