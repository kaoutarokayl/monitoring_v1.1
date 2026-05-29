const stripSecurityHeaders = (proxy) => {
  proxy.on('proxyRes', (proxyRes) => {
    delete proxyRes.headers['x-frame-options'];
    delete proxyRes.headers['X-Frame-Options'];
    delete proxyRes.headers['content-security-policy'];
    delete proxyRes.headers['Content-Security-Policy'];
  });
};

module.exports = {
  // Grafana — forwarde /grafana/* vers localhost:3000/grafana/* (serve_from_sub_path)
  '/grafana': {
    target: 'http://localhost:3000',
    secure: false,
    changeOrigin: true,
    configure: stripSecurityHeaders
  },
  // Assets Grafana statiques
  '/public': {
    target: 'http://localhost:3000',
    secure: false,
    changeOrigin: true,
    configure: stripSecurityHeaders
  },
  // Grafana REST API
  '/api': {
    target: 'http://localhost:3000',
    secure: false,
    changeOrigin: true,
    configure: stripSecurityHeaders
  },
  // Grafana 13 k8s-style APIs
  '/apis': {
    target: 'http://localhost:3000',
    secure: false,
    changeOrigin: true,
    configure: stripSecurityHeaders
  }
};
