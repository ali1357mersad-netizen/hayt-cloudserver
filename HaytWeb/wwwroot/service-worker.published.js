const CACHE_VERSION = 'hayt-pwa-v2';
const CORE_ASSETS = [
    '/',
    '/index.html',
    '/manifest.webmanifest',
    '/offline',
    '/install',
    '/css/hayt-responsive.css',
    '/css/hayt-pages.css',
    '/css/hayt-auth.css',
    '/css/hayt-pwa.css',
    '/js/hayt-pwa.js',
    '/icons/icon-192.svg',
    '/icons/icon-512.svg',
    '/icons/maskable-icon.svg'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_VERSION)
            .then(cache => cache.addAll(CORE_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys
                    .filter(key => key !== CACHE_VERSION)
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;

    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);

    if (url.pathname.startsWith('/hubs/') || url.pathname.startsWith('/api/')) {
        return;
    }

    event.respondWith(
        caches.match(request)
            .then(cached => {
                if (cached) {
                    return cached;
                }

                return fetch(request)
                    .then(response => {
                        const responseClone = response.clone();

                        if (response.ok && url.origin === self.location.origin) {
                            caches.open(CACHE_VERSION)
                                .then(cache => cache.put(request, responseClone));
                        }

                        return response;
                    })
                    .catch(() => {
                        if (request.mode === 'navigate') {
                            return caches.match('/offline')
                                .then(offline => offline || caches.match('/index.html'));
                        }

                        return Response.error();
                    });
            })
    );
});
