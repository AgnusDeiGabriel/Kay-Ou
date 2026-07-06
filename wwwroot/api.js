
const API = {
  BASE_URL: window.location.origin + '/api',

  // ── Token management ─────────────────────────────────────
  getToken()        { return sessionStorage.getItem('ka_token'); },
  getRefresh()      { return sessionStorage.getItem('ka_refresh'); },
  getUser()         { const u = sessionStorage.getItem('ka_user'); return u ? JSON.parse(u) : null; },
  isAdmin()         { const u = this.getUser(); return u?.role === 'Admin'; },
  isLoggedIn()      { return !!this.getToken(); },

  saveAuth(data) {
    sessionStorage.setItem('ka_token',   data.accessToken);
    sessionStorage.setItem('ka_refresh', data.refreshToken);
    sessionStorage.setItem('ka_user',    JSON.stringify(data.user));
  },
  clearAuth() {
    sessionStorage.removeItem('ka_token');
    sessionStorage.removeItem('ka_refresh');
    sessionStorage.removeItem('ka_user');
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

  // Pou fichye prive (KYC/dokiman pwopriyete) — retounen yon Blob olye JSON,
  // paske sa yo mande yon header Authorization ke yon <img>/window.open pa ka voye.
  async reqBlob(path) {
    const headers = {};
    const token = this.getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    let res = await fetch(this.BASE_URL + path, { headers });

    if (res.status === 401 && this.getRefresh()) {
      const refreshed = await this.refreshToken();
      if (refreshed) {
        headers['Authorization'] = `Bearer ${this.getToken()}`;
        res = await fetch(this.BASE_URL + path, { headers });
      }
    }

    if (!res.ok) throw new Error(`Erè ${res.status}`);
    return res.blob();
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
    register:       (d)  => API.req('POST',  '/auth/register', d),
    login:          (d)  => API.req('POST',  '/auth/login',    d),
    logout:         (rt) => API.req('POST',  '/auth/logout',   { refreshToken: rt }),
    me:             ()   => API.req('GET',   '/auth/me'),
    updateProfile:  (d)  => API.req('PATCH', '/auth/profile',  d),
    changePassword: (d)  => API.req('PATCH', '/auth/password', d),
    uploadAvatar(file) {
      const fd = new FormData();
      fd.append('file', file);
      return API.req('POST', '/auth/avatar', fd, true);
    },
  },

  // ── HOUSES ───────────────────────────────────────────────
  houses: {
    list:    (params = {}) => API.req('GET',    '/houses?' + new URLSearchParams(params)),
    get:     (id)          => API.req('GET',    `/houses/${id}`),
    create:  (d)           => API.req('POST',   '/houses', d),
    update:  (id, d)       => API.req('PUT',    `/houses/${id}`, d),
    delete:  (id)          => API.req('DELETE', `/houses/${id}`),
    review:  (id, d)       => API.req('PATCH',  `/houses/${id}/review`, d),
    my:      ()            => API.req('GET',    '/houses/my'),
    adminAll:(params = {}) => API.req('GET',    '/houses/admin/all?' + new URLSearchParams(params)),

    uploadImages(houseId, files) {
      const fd = new FormData();
      for (const f of files) fd.append('files', f);
      return API.req('POST', `/houses/${houseId}/images`, fd, true);
    },
    toggleLike: (id) => API.req('POST', `/houses/${id}/like`),
  },

  // ── PAYMENTS ─────────────────────────────────────────────
  payments: {
    pay(houseId, resiFile, telefon, paymentMethod) {
      const fd = new FormData();
      fd.append('resi', resiFile);
      if (telefon)       fd.append('telefon',       telefon);
      if (paymentMethod) fd.append('paymentMethod', paymentMethod);
      return API.req('POST', `/payments/${houseId}`, fd, true);
    },
    mine:    ()          => API.req('GET',   '/payments/my'),
    adminAll:(p = {})    => API.req('GET',   '/payments/admin/all?' + new URLSearchParams(p)),
    review:  (id, d)     => API.req('PATCH', `/payments/${id}/review`, d),
  },

  // ── CHAT ─────────────────────────────────────────────────
  chat: {
    getMessages: (houseId, withUserId) => {
      const q = withUserId ? `?with=${withUserId}` : '';
      return API.req('GET', `/chat/${houseId}${q}`);
    },
    sendMessage: (houseId, content, withUserId) => {
      const q = withUserId ? `?with=${withUserId}` : '';
      return API.req('POST', `/chat/${houseId}${q}`, { content });
    },
    conversations: () => API.req('GET', '/chat/conversations'),
    editMessage:   (houseId, msgId, content) => API.req('PATCH',  `/chat/${houseId}/${msgId}`,      { content }),
    deleteMessage: (houseId, msgId)          => API.req('DELETE', `/chat/${houseId}/${msgId}`),
    pinMessage:    (houseId, msgId)          => API.req('PATCH',  `/chat/${houseId}/${msgId}/pin`),
    sendVoice: (houseId, file, withUserId) => {
      const q  = withUserId ? `?with=${withUserId}` : '';
      const fd = new FormData();
      fd.append('audio', file);
      return API.req('POST', `/chat/${houseId}/voice${q}`, fd, true);
    },
  },

  // ── ADMIN ─────────────────────────────────────────────────
  admin: {
    stats:        ()     => API.req('GET',    '/admin/stats'),
    users:        (p={}) => API.req('GET',    '/admin/users?' + new URLSearchParams(p)),
    toggleActive: (id)   => API.req('PATCH',  `/admin/users/${id}/toggle-active`),
    deleteUser:   (id)   => API.req('DELETE', `/admin/users/${id}`),
  },

  // ── KYC (verifikasyon idantite) ────────────────────────────
  kyc: {
    submit(idDocument, selfie) {
      const fd = new FormData();
      fd.append('idDocument', idDocument);
      fd.append('selfie', selfie);
      return API.req('POST', '/kyc/submit', fd, true);
    },
    status:   ()        => API.req('GET',   '/kyc/status'),
    adminAll: (p = {})  => API.req('GET',   '/kyc/admin/all?' + new URLSearchParams(p)),
    review:   (userId, d) => API.req('PATCH', `/kyc/admin/${userId}/review`, d),
    docUrl:    (userId) => `/kyc/admin/${userId}/document`,
    selfieUrl: (userId) => `/kyc/admin/${userId}/selfie`,
  },

  // ── VERIFIKASYON PWOPRIYETE ─────────────────────────────────
  ownership: {
    submit(houseId, files, fields) {
      const fd = new FormData();
      fd.append('titleDeed', files.deed);
      if (files.mandate)    fd.append('mandate', files.mandate);
      if (files.taxReceipt) fd.append('taxReceipt', files.taxReceipt);
      if (fields?.adresseComplete) fd.append('adresseComplete', fields.adresseComplete);
      if (fields?.latitude != null)  fd.append('latitude', fields.latitude);
      if (fields?.longitude != null) fd.append('longitude', fields.longitude);
      return API.req('POST', `/houses/${houseId}/ownership-docs`, fd, true);
    },
    adminAll: (p = {}) => API.req('GET',   '/houses/admin/ownership-all?' + new URLSearchParams(p)),
    review:   (houseId, d) => API.req('PATCH', `/houses/${houseId}/ownership-review`, d),
    docUrl:   (houseId, docType) => `/houses/admin/${houseId}/ownership-doc/${docType}`,
  },

  // ── RAPÒ ─────────────────────────────────────────────────────
  reports: {
    create:   (d)       => API.req('POST',  '/reports', d),
    adminAll: (p = {})  => API.req('GET',   '/reports/admin/all?' + new URLSearchParams(p)),
    review:   (id, d)   => API.req('PATCH', `/reports/admin/${id}/review`, d),
  }
};