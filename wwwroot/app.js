
const State = {
  curTab: 'all',
  curZone: '',
  curSearch: '',
  curPage: 1,
  totalPages: 1,
  houses: [],
  selectedHouse: null,
  selectedFiles: [],
  resiFile: null,
  myPayments: [],
};


const $ = id => document.getElementById(id);
const show = id => $(id).style.display = '';
const hide = id => $(id).style.display = 'none';


let toastTimer;
function toast(msg, type = 'info', dur = 3000) {
  const el = $('toast');
  el.textContent = msg;
  el.className = `toast show toast-${type}`;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), dur);
}


function goTo(id) {
  document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
  $(id).classList.add('active');
  window.scrollTo(0, 0);
  if (id === 'splash') restartSmokeAnims();
}

function restartSmokeAnims() {
  document.querySelectorAll('.smoke').forEach(el => {
    el.style.animation = 'none';
    void el.offsetWidth;
    el.style.animation = '';
  });
}


function initSplash() {
  // Tcheke OAuth callback
  const params = new URLSearchParams(window.location.search);
  const kaToken = params.get('ka_token'), kaRefresh = params.get('ka_refresh'), kaUser = params.get('ka_user');
  if (kaToken && kaRefresh && kaUser) {
    try {
      const user = JSON.parse(decodeURIComponent(kaUser));
      API.saveAuth({ accessToken: kaToken, refreshToken: kaRefresh, user });
      window.history.replaceState({}, '', '/');
      if (user.role === 'Admin') { window.location.href = '/admin'; return; }
      toast('Byenveni ' + user.nom + ' 👋', 'ok');
      updateHomeUser(); goTo('home'); loadHouses(); return;
    } catch {}
  }
  if (params.get('oauth') === 'error') {
    window.history.replaceState({}, '', '/');
    toast('Koneksyon sosyal echwe. Eseye ankò.', 'err');
  }

  if (API.isLoggedIn()) {
    if (API.isAdmin()) { window.location.href = '/admin'; return; }
    goTo('home'); loadHouses(); updateHomeUser();
  }
}

let _googleReady = false;

async function initGoogleSignIn() {
  try {
    const res = await API.req('GET', '/auth/config');
    const clientId = res.data?.googleClientId;
    if (!clientId || typeof google === 'undefined') return;
    google.accounts.id.initialize({
      client_id: clientId,
      callback: async (response) => {
        try {
          const r = await API.req('POST', '/auth/google-token', { credential: response.credential });
          API.saveAuth(r.data);
          if (r.data.user.role === 'Admin') { window.location.href = '/admin'; return; }
          toast('Byenveni ' + r.data.user.nom + ' 👋', 'ok');
          updateHomeUser(); goTo('home'); loadHouses();
        } catch (e) { toast(e.message, 'err'); }
      }
    });
    _googleReady = true;
  } catch {}
}

function socialLogin(provider) {
  if (provider === 'Google') {
    if (!_googleReady || typeof google === 'undefined') {
      toast('Google pako prè. Eseye ankò.', 'info'); return;
    }
    google.accounts.id.prompt(); return;
  }
  window.location.href = '/api/auth/signin/' + provider;
}

async function loadSocialProviders() {
  try {
    const res = await API.req('GET', '/auth/providers');
    const active = new Set(res.data || []);
    document.querySelectorAll('.auth-social-btn').forEach(btn => {
      const key = btn.classList.contains('auth-google') ? 'google'
                : btn.classList.contains('auth-apple')  ? 'apple' : 'facebook';
      btn.disabled = !active.has(key);
      btn.style.opacity = active.has(key) ? '1' : '0.35';
      btn.title = active.has(key) ? key.charAt(0).toUpperCase()+key.slice(1) : 'Pa konfigire ankò';
    });
  } catch {}
}


function showRegister() {
  show('reg-form'); hide('login-form');
  loadSocialProviders();
}
function showLogin() {
  hide('reg-form'); show('login-form');
  loadSocialProviders();
}

async function doRegister() {
  const nom    = $('reg-nom').value.trim();
  const email  = $('reg-email').value.trim();
  const tel    = $('reg-tel').value.trim();
  const pass   = $('reg-pass').value;

  if (!nom || !email || !tel || !pass) { toast('Ranpli tout chan yo!', 'err'); return; }
  if (pass.length < 8)                 { toast('Modpas dwe gen omwen 8 karaktè.', 'err'); return; }

  setLoading('btn-register', true);
  try {
    const res = await API.auth.register({ nom, email, telefon: tel, password: pass });
    API.saveAuth(res.data);
    toast('Kont kreye! Byenveni ' + res.data.user.nom + ' 🎉', 'ok');
    updateHomeUser();
    goTo('home');
    loadHouses();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-register', false);
  }
}

async function doLogin() {
  const email = $('login-email').value.trim();
  const pass  = $('login-pass').value;

  if (!email || !pass) { toast('Antre email ak modpas ou!', 'err'); return; }

  setLoading('btn-login', true);
  try {
    const res = await API.auth.login({ email, password: pass });
    API.saveAuth(res.data);
    if (res.data.user.role === 'Admin') { window.location.href = '/admin'; return; }
    toast('Byenveni ' + res.data.user.nom + ' 👋', 'ok');
    updateHomeUser();
    goTo('home');
    loadHouses();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-login', false);
  }
}

async function doLogout() {
  try {
    await API.auth.logout(API.getRefresh());
  } catch {}
  API.clearAuth();
  State.houses = [];
  goTo('splash');
}

function updateHomeUser() {
  const user = API.getUser();
  if (!user) return;
  const initials = user.nom.split(' ').map(n => n[0]).join('').toUpperCase().slice(0,2);
  $('home-avatar').textContent  = initials;
  $('profile-av').textContent   = initials;
  $('profile-name').textContent = user.nom;
  $('profile-email').textContent= user.email;
}


async function loadHouses(reset = true) {
  if (reset) { State.curPage = 1; State.houses = []; }

  const params = {
    page: State.curPage,
    pageSize: 10,
  };
  if (State.curTab !== 'all')  params.type   = State.curTab;
  if (State.curZone)           params.zone   = State.curZone;
  if (State.curSearch)         params.search = State.curSearch;

  $('listings-container').innerHTML = '<div class="loader"><div class="spinner"></div></div>';

  try {
    const res = await API.houses.list(params);
    const data = res.data;
    State.houses = reset ? data.items : [...State.houses, ...data.items];
    State.totalPages = data.totalPages;
    $('listings-count').textContent = data.total + ' kay';
    renderHouses(State.houses);
  } catch (e) {
    $('listings-container').innerHTML = `
      <div class="empty-state">
        <div class="empty-icon">⚠️</div>
        <div class="empty-title">Erè koneksyon</div>
        <div class="empty-sub">${e.message}</div>
      </div>`;
  }
}

function renderHouses(list) {
  if (!list.length) {
    $('listings-container').innerHTML = `
      <div class="empty-state">
        <div class="empty-icon">🏚️</div>
        <div class="empty-title">Pa gen kay pou montre</div>
        <div class="empty-sub">Eseye chanje filtre ou oswa ajoute yon nouvo kay.</div>
      </div>`;
    return;
  }

  $('listings-container').innerHTML = list.map(h => {
    const img = h.images && h.images[0]
      ? `<img src="${API.BASE_URL.replace('/api','')}${h.images[0]}" alt="${h.titre}">`
      : `<div class="card-img-placeholder">${h.type === 'Lwe' ? '🏢' : '🏡'}</div>`;
    const priceFmt = h.type === 'Lwe'
      ? `$${h.prix.toLocaleString()} <span>/mwa</span>`
      : `$${h.prix.toLocaleString()} <span>USD</span>`;

    return `
      <div class="house-card" onclick="openDetail(${h.id})">
        <div class="card-img" style="background:${h.type==='Lwe'?'linear-gradient(135deg,#D4E8C9,#9FC98A)':'linear-gradient(135deg,#E8D5C4,#C9A98A)'}">
          ${img}
          <span class="card-badge badge-${h.type.toLowerCase()}">${h.type === 'Vann' ? 'Pou Vann' : 'Pou Lwe'}</span>
          ${h.statut === 'Approved' ? '<span class="badge-verified">✓ Verifye</span>' : ''}
        </div>
        <div class="card-body">
          <div class="card-title">${h.titre}</div>
          <div class="card-zone">📍 ${h.zone}</div>
          <div class="card-price">${priceFmt}</div>
          <div class="card-stats">
            <div class="card-stat"><b>${h.chambres}</b> Chanm</div>
            <div class="card-stat"><b>${h.beny}</b> Beny</div>
            <div class="card-stat"><b>${h.superficie}</b> m²</div>
          </div>
        </div>
      </div>`;
  }).join('');
}

function switchTab(el, val) {
  document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
  el.classList.add('active');
  State.curTab = val;
  loadHouses();
}

function toggleCustomZone() {
  const sel = $('add-zone');
  const custom = $('add-zone-custom');
  if (sel.value === '__autre__') {
    custom.style.display = '';
    custom.focus();
  } else {
    custom.style.display = 'none';
    custom.value = '';
  }
}

function switchChip(el, val) {
  document.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  State.curZone = val;
  loadHouses();
}

let searchTimer;
function onSearch(val) {
  clearTimeout(searchTimer);
  State.curSearch = val;
  searchTimer = setTimeout(() => loadHouses(), 400);
}


async function openDetail(id) {
  try {
    const res = await API.houses.get(id);
    const h = res.data;
    State.selectedHouse = h;

    // Hero image
    const heroEl = $('det-hero-inner');
    if (h.images && h.images[0]) {
      heroEl.innerHTML = `<img src="${API.BASE_URL.replace('/api','')}${h.images[0]}" style="width:100%;height:100%;object-fit:cover;">`;
    } else {
      heroEl.innerHTML = `<div class="detail-hero-placeholder">${h.type==='Lwe'?'🏢':'🏡'}</div>`;
      $('det-hero').style.background = h.type === 'Lwe'
        ? 'linear-gradient(135deg,#D4E8C9,#9FC98A)'
        : 'linear-gradient(135deg,#E8D5C4,#C9A98A)';
    }

    $('det-title').textContent   = h.titre;
    $('det-zone').textContent    = '📍 ' + h.zone;
    $('det-price').innerHTML     = h.type === 'Lwe'
      ? `$${h.prix.toLocaleString()} <span>/mwa</span>`
      : `$${h.prix.toLocaleString()} <span>USD</span>`;
    $('det-ch').textContent      = h.chambres;
    $('det-bn').textContent      = h.beny;
    $('det-sf').textContent      = h.superficie;
    $('det-desc').textContent    = h.description;

    const initials = h.owner.nom.split(' ').map(n=>n[0]).join('').toUpperCase().slice(0,2);
    $('det-owner-av').textContent   = initials;
    $('det-owner-name').textContent = h.owner.nom;

    const badge = $('det-badge');
    badge.textContent   = h.type === 'Vann' ? 'Pou Vann' : 'Pou Lwe';
    badge.className     = `card-badge badge-${h.type.toLowerCase()}`;

    // Amenities
    const amens = [];
    if (h.garaj)    amens.push('🚗 Garaj');
    if (h.sekirite) amens.push('🔒 Sekirite');
    if (h.piscine)  amens.push('🏊 Pisin');
    $('det-amenities').textContent = amens.join(' • ') || '';

    // Si itilizatè deja peye: montre enfòmasyon kontak
    if (h.userHasPaid) {
      $('cta-pay').textContent = '📞 ' + h.owner.telefon;
      $('cta-pay').style.background = 'var(--green)';
    } else {
      $('cta-pay').textContent = '💳 Peye pou Kontak';
      $('cta-pay').style.background = '';
    }

    goTo('detail');
  } catch (e) {
    toast(e.message, 'err');
  }
}


$('add-file-input').addEventListener('change', function () {
  State.selectedFiles = Array.from(this.files);
  if (State.selectedFiles.length) {
    $('upload-zone').classList.add('has-files');
    $('upload-txt').textContent = `${State.selectedFiles.length} foto chwazi ✓`;
  }
});

function selectType(el) {
  document.querySelectorAll('.type-btn').forEach(b => b.classList.remove('sel'));
  el.classList.add('sel');
}

async function submitHouse() {
  if (!API.isLoggedIn()) { showLogin(); return; }

  const titre       = $('add-titre').value.trim();
  const description = $('add-desc').value.trim();
  const prix        = parseFloat($('add-prix').value);
  const zoneSelect  = $('add-zone').value;
  const zone        = zoneSelect === '__autre__' ? $('add-zone-custom').value.trim() : zoneSelect.trim();
  const chambres    = parseInt($('add-chambres').value) || 1;
  const beny        = parseInt($('add-beny').value) || 1;
  const superficie  = parseInt($('add-superficie').value) || 50;
  const typeEl      = document.querySelector('.type-btn.sel');
  const type        = typeEl ? (typeEl.dataset.type || 'Vann') : 'Vann';
  const garaj       = $('amen-garaj').classList.contains('on');
  const sekirite    = $('amen-sekirite').classList.contains('on');
  const piscine     = $('amen-pisin').classList.contains('on');

  if (!titre || !description || !prix || isNaN(prix)) {
    toast('Ranpli tout chan obligatwa yo!', 'err'); return;
  }
  if (!zone) {
    toast('Tanpri ekri non zòn lan!', 'err'); return;
  }

  setLoading('btn-submit-house', true);
  try {
    const res = await API.houses.create({ titre, description, prix, zone, type, chambres, beny, superficie, garaj, sekirite, piscine });
    const houseId = res.data.id;

    // Upload foto si gen
    if (State.selectedFiles.length > 0) {
      await API.houses.uploadImages(houseId, State.selectedFiles);
    }

    toast('Kay voye pou validasyon admin! ✓', 'ok');
    // Reset form
    $('add-titre').value = '';
    $('add-desc').value  = '';
    $('add-prix').value  = '';
    State.selectedFiles  = [];
    $('upload-zone').classList.remove('has-files');
    $('upload-txt').textContent = 'Pran foto oswa chwazi nan galri';
    goTo('home');
    loadHouses();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-submit-house', false);
  }
}


$('resi-input').addEventListener('change', function () {
  State.resiFile = this.files[0] || null;
  if (State.resiFile) {
    $('resi-upload').classList.add('has-file');
    $('resi-txt').textContent = 'Resi chwazi ✓ — ' + State.resiFile.name;
  }
});

function showPayment() {
  if (!API.isLoggedIn()) { showLogin(); return; }
  const h = State.selectedHouse;
  if (!h) return;
  $('pay-house-name').textContent = h.titre;
  $('pay-house-zone').textContent = h.zone;
  goTo('payment');
}

function selectMethod(el) {
  document.querySelectorAll('.pay-method').forEach(m => m.classList.remove('sel'));
  el.classList.add('sel');
}

async function submitPayment() {
  if (!State.resiFile) { toast('Upload foto resi ou!', 'err'); return; }
  const h = State.selectedHouse;
  if (!h) return;
  const telefon = $('pay-tel').value.trim();

  setLoading('btn-submit-pay', true);
  try {
    await API.payments.pay(h.id, State.resiFile, telefon);
    // Show success
    $('pay-form-area').style.display = 'none';
    $('pay-success').style.display   = 'flex';
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-submit-pay', false);
  }
}


async function loadMyPayments() {
  if (!API.isLoggedIn()) return;
  $('my-payments-list').innerHTML = '<div class="loader"><div class="spinner"></div></div>';
  try {
    const res = await API.payments.mine();
    const list = res.data;
    if (!list.length) {
      $('my-payments-list').innerHTML = `<div class="empty-state" style="padding:30px 20px;"><div class="empty-icon">💳</div><div class="empty-sub">Pa gen peman ankò.</div></div>`;
      return;
    }
    $('my-payments-list').innerHTML = list.map(p => `
      <div class="pay-list-item">
        <div class="pay-list-top">
          <div class="pay-list-house">${p.house.titre}</div>
          <span class="pill pill-${p.statut.toLowerCase()}">${p.statut}</span>
        </div>
        <div class="pay-list-sub">${p.house.zone} • ${p.amount.toLocaleString()} HTG • ${new Date(p.createdAt).toLocaleDateString('fr-HT')}</div>
      </div>`).join('');
  } catch (e) {
    $('my-payments-list').innerHTML = `<div class="empty-state"><div class="empty-sub">${e.message}</div></div>`;
  }
}


function setLoading(btnId, on) {
  const btn = $(btnId);
  if (!btn) return;
  btn.disabled = on;
  btn.dataset.origText = btn.dataset.origText || btn.textContent;
  btn.textContent = on ? '⏳ Tann...' : btn.dataset.origText;
}


function openInfoModal()  { $('info-modal').classList.add('open'); }
function closeInfoModal() { $('info-modal').classList.remove('open'); }

document.addEventListener('DOMContentLoaded', () => {
  initSplash();
  initGoogleSignIn();
});