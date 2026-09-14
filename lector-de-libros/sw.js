// Minimal service worker — just enough for Chrome/Android to consider this app installable.
// No offline caching promises here; it just proxies network requests.
self.addEventListener('install', function(e){ self.skipWaiting(); });
self.addEventListener('activate', function(e){ e.waitUntil(self.clients.claim()); });
self.addEventListener('fetch', function(e){
  // For the page itself (and any navigation), always go to the network and skip the
  // browser's HTTP cache — otherwise an installed app can keep showing an old version
  // for a long time even after a new one is deployed. Other requests (fonts, cdn libs)
  // keep normal caching since those rarely change.
  var isNavigation = e.request.mode === 'navigate' || (e.request.destination === 'document');
  if (isNavigation){
    e.respondWith(
      fetch(e.request, { cache: 'no-store' }).catch(function(){ return caches.match(e.request); })
    );
    return;
  }
  e.respondWith(
    fetch(e.request).catch(function(){ return caches.match(e.request); })
  );
});
