self.addEventListener('install', event => {
    console.info('Hayt development service worker installed.');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.info('Hayt development service worker activated.');
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', event => {
    // Development mode: no aggressive caching.
});
