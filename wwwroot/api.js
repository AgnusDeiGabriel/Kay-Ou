
const API = {
  BASE_URL: 'http://localhost:5000/api',

  // ── Token management ─────────────────────────────────────
  getToken()        { return localStorage.getItem('ka_token'); },
  getRefresh()      { return localStorage.getItem('ka_refresh'); },
  getUser()         { const u = localStorage.getItem('ka_user'); return u ? JSON.parse(u) : null; },
  isAdmin()         { const u = this.getUser(); return u?.role === 'Admin'; },
  isLoggedIn()      { return !!this.getToken(); },

  saveAuth(data) {
    localStorage.setItem('ka_token',   data.accessToken);
    localStorage.setItem('ka_refresh', data.refreshToken);
    localStorage.setItem('ka_user',    JSON.stringify(data.user));
  },
  clearAuth() {
    localStorage.removeItem('ka_token');
    localStorage.removeItem('ka_refresh');
    localStorage.removeItem('ka_user');
  },

  // ── Core fetch wrapper ────────────────────────────────────
  async req(method, path, body = null, isForm = false) {
    const headers = {};
    const token = this.getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (!isForm && body) headers['Content-Type'] = 'application/json';

    const opts = { method, headers };
    if (body) opts.body = isForm ? body : JSON.stringify(body);

    let res = await fetch(this.BASE_URL + path, opts);

    // Auto-refresh si 401
    if (res.status === 401 && this.getRefresh()) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        headers['Authorization'] = `Bearer ${this.getToken()}`;
        res = await fetch(this.BASE_URL + path, { ...opts, headers });
      }
    }

    const json = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(json.message || `Erè ${res.status}`);
    return json;
  },

  async refreshToken() {
    try {
      const res = await fetch(this.BASE_URL + '/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: this.getRefresh() })
      });
      if (!res.ok) { this.clearAuth(); return false; }
      const json = await res.json();
      this.saveAuth(json.data);
      return true;
    } catch { this.clearAuth(); return false; }
  },

  // ── AUTH ─────────────────────────────────────────────────
  auth: {
    register: (d)  => API.req('POST', '/auth/register', d),
    login:    (d)  => API.req('POST', '/auth/login',    d),
    logout:   (rt) => API.req('POST', '/auth/logout',   { refreshToken: rt }),
  },

  // ── HOUSES ───────────────────────────────────────────────
  houses: {
    list:    (params = {}) => API.req('GET',    '/houses?' + new URLSearchParams(params)),
    get:     (id)          => API.req('GET',    `/houses/${id}`),
    create:  (d)           => API.req('POST',   '/houses', d),
    update:  (id, d)       => API.req('PUT',    `/houses/${id}`, d),
    delete:  (id)          => API.req('DELETE', `/houses/${id}`),
    review:  (id, d)       => API.req('PATCH',  `/houses/${id}/review`, d),
    adminAll:(params = {}) => API.req('GET',    '/houses/admin/all?' + new URLSearchParams(params)),

    uploadImages(houseId, files) {
      const fd = new FormData();
      for (const f of files) fd.append('files', f);
      return API.req('POST', `/houses/${houseId}/images`, fd, true);
    }
  },

  // ── PAYMENTS ─────────────────────────────────────────────
  payments: {
    pay(houseId, resiFile, telefon) {
      const fd = new FormData();
      fd.append('resi', resiFile);
      if (telefon) fd.append('telefon', telefon);
      return API.req('POST', `/payments/${houseId}`, fd, true);
    },
    mine:    ()          => API.req('GET',   '/payments/my'),
    adminAll:(p = {})    => API.req('GET',   '/payments/admin/all?' + new URLSearchParams(p)),
    review:  (id, d)     => API.req('PATCH', `/payments/${id}/review`, d),
  },

  // ── ADMIN ─────────────────────────────────────────────────
  admin: {
    stats:        ()     => API.req('GET',   '/admin/stats'),
    users:        (p={}) => API.req('GET',   '/admin/users?' + new URLSearchParams(p)),
    toggleActive: (id)   => API.req('PATCH', `/admin/users/${id}/toggle-active`),
  }
};