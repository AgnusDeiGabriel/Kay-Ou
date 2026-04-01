/* ============================================================
   KayAyiti Admin — Main JS (konekte ak ASP.NET Core API)
   ============================================================ */

// ── DOM helpers ──────────────────────────────────────────────
const $  = id => document.getElementById(id);
const el = (tag, attrs = {}, html = '') => {
  const e = document.createElement(tag);
  Object.entries(attrs).forEach(([k,v]) => e.setAttribute(k,v));
  e.innerHTML = html;
  return e;
};

// ── Toast ────────────────────────────────────────────────────
let toastTimer;
function toast(msg, type = 'info', dur = 3500) {
  const t = $('toast');
  t.textContent = msg;
  t.className = `toast show toast-${type}`;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => t.classList.remove('show'), dur);
}

// ── Loader helpers ────────────────────────────────────────────
function setLoading(btnId, on, origTxt) {
  const btn = $(btnId);
  if (!btn) return;
  btn.disabled = on;
  if (on) { btn.dataset.orig = btn.textContent; btn.textContent = '⏳ Tann...'; }
  else btn.textContent = origTxt || btn.dataset.orig || btn.textContent;
}

// ── Page navigation ──────────────────────────────────────────
function showPage(pageId, navEl) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.sb-item').forEach(i => i.classList.remove('active'));
  $(pageId).classList.add('active');
  if (navEl) navEl.classList.add('active');

  // Chaje done pou paj la
  const loaders = {
    'page-dashboard': loadDashboard,
    'page-houses':    loadHouses,
    'page-payments':  loadPayments,
    'page-users':     loadUsers,
  };
  if (loaders[pageId]) loaders[pageId]();
}

// ── AUTH ─────────────────────────────────────────────────────
async function doAdminLogin() {
  const email = $('l-email').value.trim();
  const pass  = $('l-pass').value;
  if (!email || !pass) { $('login-error').style.display = 'block'; $('login-error').textContent = 'Antre email ak modpas ou!'; return; }
  $('login-error').style.display = 'none';
  setLoading('btn-login', true);
  try {
    const res = await API.auth.login({ email, password: pass });
    if (res.data.user.role !== 'Admin') {
      API.clearAuth(); throw new Error('Ou pa admin. Aksè refize.');
    }
    API.saveAuth(res.data);
    $('login-page').style.display = 'none';
    $('app').classList.add('show');
    updateSidebarUser();
    loadDashboard();
    toast('Byenveni Admin! 👋', 'ok');
  } catch (e) {
    $('login-error').style.display = 'block';
    $('login-error').textContent = e.message;
  } finally {
    setLoading('btn-login', false, 'Konekte kòm Admin →');
  }
}

function doLogout() {
  API.auth.logout(API.getRefresh()).catch(() => {});
  API.clearAuth();
  $('app').classList.remove('show');
  $('login-page').style.display = 'flex';
  toast('Ou dekonekte.', 'info');
}

function updateSidebarUser() {
  const u = API.getUser();
  if (!u) return;
  $('sb-name').textContent = u.nom;
  $('sb-role').textContent = u.email;
}

// ── DASHBOARD ────────────────────────────────────────────────
async function loadDashboard() {
  try {
    const res = await API.admin.stats();
    const s   = res.data;

    $('stat-users').textContent         = s.totalUsers;
    $('stat-pending-houses').textContent = s.housesPending;
    $('stat-pending-pay').textContent    = s.paymentsPending;
    $('stat-revenue').textContent        = s.totalRevenueHTG.toLocaleString();

    // Update sidebar badges
    if ($('badge-houses'))   $('badge-houses').textContent   = s.housesPending   || '';
    if ($('badge-payments')) $('badge-payments').textContent = s.paymentsPending || '';

    // Dènye kay pou valide
    renderDashHouses();
    renderDashPayments();
  } catch (e) {
    toast('Erè chajman dashboard: ' + e.message, 'err');
  }
}

async function renderDashHouses() {
  const tbody = $('dash-houses');
  tbody.innerHTML = '<tr><td colspan="5"><div class="loader"><div class="spinner"></div></div></td></tr>';
  try {
    const res = await API.houses.adminAll({ statut: 'Pending', page: 1, pageSize: 5 });
    const list = res.data.items;
    if (!list.length) {
      tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:var(--muted);padding:20px;">Pa gen kay pou valide 🎉</td></tr>';
      return;
    }
    tbody.innerHTML = list.map(h => `
      <tr>
        <td>${houseCell(h)}</td>
        <td>${userCell(h.owner)}</td>
        <td>$${h.prix.toLocaleString()}</td>
        <td><span class="pill pill-pending">Pandan</span></td>
        <td>
          <div class="action-row">
            <button class="btn btn-sm btn-approve" onclick="houseReview(${h.id},'Approved',this)">✓ Apwouve</button>
            <button class="btn btn-sm btn-reject"  onclick="houseReview(${h.id},'Rejected',this)">✕</button>
          </div>
        </td>
      </tr>`).join('');
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="5" style="color:var(--red);padding:16px;">${e.message}</td></tr>`;
  }
}

async function renderDashPayments() {
  const cont = $('dash-payments');
  cont.innerHTML = '<div class="loader"><div class="spinner"></div></div>';
  try {
    const res  = await API.payments.adminAll({ statut: 'Pending', page: 1, pageSize: 5 });
    const list = res.data.items;
    if (!list.length) {
      cont.innerHTML = '<div style="padding:20px;text-align:center;color:var(--muted);">Pa gen peman pandan 🎉</div>';
      return;
    }
    cont.innerHTML = list.map(p => `
      <div class="mini-item" onclick="openPayModal(${JSON.stringify(p).replace(/"/g,'&quot;')})">
        ${userAv(p.user)}
        <div>
          <div class="user-name">${p.user.nom}</div>
          <div class="user-email">${p.house.titre} • ${p.statut === 'Verified' ? 'MonCash' : 'MonCash/NatCash'}</div>
        </div>
        <div class="mini-amount">${p.amount.toLocaleString()} HTG</div>
        <div class="mini-time">${fmtDate(p.createdAt)}</div>
        <span class="pill pill-${p.statut.toLowerCase()}" style="margin-left:10px;">${p.statut}</span>
      </div>`).join('');
  } catch (e) {
    cont.innerHTML = `<div style="padding:16px;color:var(--red);">${e.message}</div>`;
  }
}

// ── HOUSES PAGE ───────────────────────────────────────────────
let houseFilters = { statut: '', zone: '', search: '', page: 1, pageSize: 20 };

async function loadHouses() {
  const tbody = $('houses-tbody');
  tbody.innerHTML = '<tr><td colspan="8"><div class="loader"><div class="spinner"></div></div></td></tr>';
  try {
    const params = { page: houseFilters.page, pageSize: houseFilters.pageSize };
    if (houseFilters.statut) params.statut = houseFilters.statut;
    if (houseFilters.zone)   params.zone   = houseFilters.zone;
    if (houseFilters.search) params.search = houseFilters.search;

    const res  = await API.houses.adminAll(params);
    const data = res.data;
    $('house-count-lbl').textContent = data.total + ' kay';

    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="8"><div class="empty-state"><div class="empty-icon">🏚️</div><div class="empty-sub">Pa gen kay pou montre.</div></div></td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(h => `
      <tr>
        <td>${houseCell(h)}</td>
        <td>${userCell(h.owner)}</td>
        <td>${h.zone}</td>
        <td>$${h.prix.toLocaleString()}${h.type === 'Lwe' ? '/mwa' : ''}</td>
        <td>${h.type}</td>
        <td>${fmtDate(h.createdAt)}</td>
        <td><span class="pill pill-${h.statut.toLowerCase()}">${h.statut}</span></td>
        <td>
          <div class="action-row">
            ${h.statut !== 'Approved' ? `<button class="btn btn-sm btn-approve" onclick="houseReview(${h.id},'Approved',this)">✓</button>` : ''}
            ${h.statut !== 'Rejected' ? `<button class="btn btn-sm btn-reject"  onclick="houseReview(${h.id},'Rejected',this)">✕</button>` : ''}
          </div>
        </td>
      </tr>`).join('');
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="8" style="color:var(--red);padding:16px;">${e.message}</td></tr>`;
  }
}

async function houseReview(id, action, btn) {
  btn.disabled = true;
  try {
    await API.houses.review(id, { action });
    toast(action === 'Approved' ? 'Kay apwouve ✓' : 'Kay rejte.', action === 'Approved' ? 'ok' : 'err');
    loadHouses();
    if ($('page-dashboard').classList.contains('active')) renderDashHouses();
  } catch (e) {
    toast(e.message, 'err');
    btn.disabled = false;
  }
}

let houseSearchTimer;
function onHouseSearch(val) {
  clearTimeout(houseSearchTimer);
  houseFilters.search = val;
  houseFilters.page   = 1;
  houseSearchTimer    = setTimeout(loadHouses, 400);
}
function onHouseStatusFilter(val) { houseFilters.statut = val; houseFilters.page = 1; loadHouses(); }
function onHouseZoneFilter(val)   { houseFilters.zone   = val; houseFilters.page = 1; loadHouses(); }

// ── PAYMENTS PAGE ─────────────────────────────────────────────
let payFilters = { statut: '', page: 1, pageSize: 20 };

async function loadPayments() {
  const tbody = $('payments-tbody');
  tbody.innerHTML = '<tr><td colspan="8"><div class="loader"><div class="spinner"></div></div></td></tr>';
  try {
    const params = { page: payFilters.page, pageSize: payFilters.pageSize };
    if (payFilters.statut) params.statut = payFilters.statut;

    const res  = await API.payments.adminAll(params);
    const data = res.data;
    $('pay-count-lbl').textContent = data.total + ' peman';

    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="8"><div class="empty-state"><div class="empty-icon">💳</div><div class="empty-sub">Pa gen peman pou montre.</div></div></td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(p => `
      <tr onclick="openPayModal(${JSON.stringify(p).replace(/"/g,'&quot;').replace(/'/g,'&#39;')})">
        <td>${userCell(p.user)}</td>
        <td>${p.house.titre}</td>
        <td style="font-weight:500;">${p.amount.toLocaleString()} HTG</td>
        <td>${p.resiImageUrl ? `<button class="btn btn-sm btn-amber" onclick="event.stopPropagation();viewResi('${API.BASE_URL.replace('/api','')}${p.resiImageUrl}')">📋 Wè resi</button>` : '—'}</td>
        <td>${fmtDate(p.createdAt)}</td>
        <td><span class="pill pill-${p.statut.toLowerCase()}">${p.statut}</span></td>
        <td>
          <div class="action-row" onclick="event.stopPropagation()">
            ${p.statut === 'Pending' ? `
              <button class="btn btn-sm btn-verify" onclick="payReview(${p.id},'Verified',this)">✓ Verifye</button>
              <button class="btn btn-sm btn-reject"  onclick="payReview(${p.id},'Rejected',this)">✕</button>
            ` : `<span style="font-size:12px;color:var(--muted);">Trete ✓</span>`}
          </div>
        </td>
      </tr>`).join('');
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="8" style="color:var(--red);padding:16px;">${e.message}</td></tr>`;
  }
}

async function payReview(id, action, btn) {
  btn.disabled = true;
  try {
    await API.payments.review(id, { action });
    toast(action === 'Verified' ? 'Peman verifye! Itilizatè a gen aksè kounye a ✓' : 'Peman rejte.', action === 'Verified' ? 'ok' : 'err');
    loadPayments();
    if ($('page-dashboard').classList.contains('active')) { renderDashPayments(); loadDashboard(); }
  } catch (e) {
    toast(e.message, 'err');
    btn.disabled = false;
  }
}

function onPayStatusFilter(val) { payFilters.statut = val; payFilters.page = 1; loadPayments(); }

// ── PAY MODAL ─────────────────────────────────────────────────
let currentPayId = null;
function openPayModal(p) {
  if (typeof p === 'string') p = JSON.parse(p);
  currentPayId = p.id;

  $('modal-pay-title').textContent = 'Resi — ' + p.user.nom;
  $('resi-user').textContent    = p.user.nom + ' (' + p.user.email + ')';
  $('resi-house').textContent   = p.house.titre + ' — ' + p.house.zone;
  $('resi-amount').textContent  = p.amount.toLocaleString() + ' HTG';
  $('resi-date').textContent    = fmtDate(p.createdAt);
  $('resi-status').className    = `pill pill-${p.statut.toLowerCase()}`;
  $('resi-status').textContent  = p.statut;

  const prev = $('resi-preview-inner');
  if (p.resiImageUrl) {
    prev.innerHTML = `<img src="${API.BASE_URL.replace('/api','')}${p.resiImageUrl}" style="width:100%;height:100%;object-fit:contain;">`;
  } else {
    prev.innerHTML = `<div style="font-size:40px;">🧾</div><div style="font-size:13px;color:var(--muted);">Foto resi pa disponib</div>`;
  }

  // Montre bouton si Pending
  $('modal-pay-actions').style.display = p.statut === 'Pending' ? 'flex' : 'none';
  $('reject-reason-wrap').style.display = 'none';
  $('reject-reason').value = '';

  $('modal-pay').classList.add('open');
}

function viewResi(url) {
  window.open(url, '_blank');
}

async function modalPayVerify() {
  setLoading('btn-modal-verify', true);
  try {
    await API.payments.review(currentPayId, { action: 'Verified' });
    toast('Peman verifye! Itilizatè a gen aksè kounye a ✓', 'ok');
    $('modal-pay').classList.remove('open');
    loadPayments();
    loadDashboard();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-modal-verify', false, '✓ Verifye Peman');
  }
}

async function modalPayReject() {
  const note = $('reject-reason').value.trim();
  if (!note) {
    $('reject-reason-wrap').style.display = 'block';
    $('reject-reason').focus();
    toast('Antre rezon refi a.', 'info');
    return;
  }
  setLoading('btn-modal-reject', true);
  try {
    await API.payments.review(currentPayId, { action: 'Rejected', noteAdmin: note });
    toast('Peman rejte.', 'err');
    $('modal-pay').classList.remove('open');
    loadPayments();
    loadDashboard();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-modal-reject', false, '✕ Rejte');
  }
}

// ── USERS PAGE ────────────────────────────────────────────────
let userFilters = { search: '', page: 1, pageSize: 20 };

async function loadUsers() {
  const tbody = $('users-tbody');
  tbody.innerHTML = '<tr><td colspan="7"><div class="loader"><div class="spinner"></div></div></td></tr>';
  try {
    const params = { page: userFilters.page, pageSize: userFilters.pageSize };
    if (userFilters.search) params.search = userFilters.search;

    const res  = await API.admin.users(params);
    const data = res.data;
    $('user-count-lbl').textContent = data.total + ' itilizatè';

    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="7"><div class="empty-state"><div class="empty-icon">👥</div><div class="empty-sub">Pa gen itilizatè pou montre.</div></div></td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(u => `
      <tr>
        <td>${userCell(u)}</td>
        <td>${u.telefon || '—'}</td>
        <td>—</td>
        <td>—</td>
        <td>${fmtDate(u.createdAt)}</td>
        <td><span class="pill pill-${u.isActive ? 'active' : 'inactive'}" id="pill-${u.id}">${u.isActive ? 'Aktif' : 'Dezaktive'}</span></td>
        <td>
          <div class="action-row">
            <button class="toggle ${u.isActive ? 'on' : ''}" id="toggle-${u.id}" onclick="toggleUser(${u.id},this)"></button>
          </div>
        </td>
      </tr>`).join('');
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="7" style="color:var(--red);padding:16px;">${e.message}</td></tr>`;
  }
}

async function toggleUser(id, btn) {
  btn.disabled = true;
  try {
    const res = await API.admin.toggleActive(id);
    const u   = res.data;
    btn.classList.toggle('on', u.isActive);
    const pill = $('pill-' + id);
    if (pill) {
      pill.className = `pill pill-${u.isActive ? 'active' : 'inactive'}`;
      pill.textContent = u.isActive ? 'Aktif' : 'Dezaktive';
    }
    toast(u.isActive ? 'Kont aktive ✓' : 'Kont dezaktive.', u.isActive ? 'ok' : 'err');
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    btn.disabled = false;
  }
}

let userSearchTimer;
function onUserSearch(val) {
  clearTimeout(userSearchTimer);
  userFilters.search = val;
  userFilters.page   = 1;
  userSearchTimer    = setTimeout(loadUsers, 400);
}

// ── HELPERS ───────────────────────────────────────────────────
function houseCell(h) {
  const img = h.images && h.images[0]
    ? `<img src="${API.BASE_URL.replace('/api','')}${h.images[0]}" style="width:100%;height:100%;object-fit:cover;">`
    : (h.type === 'Lwe' ? '🏢' : '🏡');
  return `<div class="house-cell">
    <div class="house-thumb">${img}</div>
    <div><div class="house-name">${h.titre}</div><div class="house-zone">${h.zone}</div></div>
  </div>`;
}

function userCell(u) {
  const initials = u.nom.split(' ').map(n=>n[0]).join('').toUpperCase().slice(0,2);
  const colors = ['#FDE8D8,var(--orange)','#D6F0E2,var(--green)','#FEF3D0,var(--amber)','#E6F1FB,#185FA5'];
  const ci = (u.id || 0) % colors.length;
  const [bg,fg] = colors[ci].split(',');
  return `<div class="user-cell">
    <div class="user-av" style="background:${bg};color:${fg};">${initials}</div>
    <div><div class="user-name">${u.nom}</div><div class="user-email">${u.email}</div></div>
  </div>`;
}

function userAv(u) {
  const initials = u.nom.split(' ').map(n=>n[0]).join('').toUpperCase().slice(0,2);
  return `<div class="user-av" style="background:var(--orange-lt);color:var(--orange);">${initials}</div>`;
}

function fmtDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('fr-HT', { day:'numeric', month:'short', year:'numeric' });
}

// ── INIT ──────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  // Si admin deja konekte
  if (API.isLoggedIn() && API.isAdmin()) {
    $('login-page').style.display = 'none';
    $('app').classList.add('show');
    updateSidebarUser();
    loadDashboard();
  }

  // Enter pou login
  ['l-email','l-pass'].forEach(id => {
    $(id).addEventListener('keydown', e => { if (e.key === 'Enter') doAdminLogin(); });
  });

  // Klik deyò modal pou fèmen
  $('modal-pay').addEventListener('click', e => { if (e.target === $('modal-pay')) $('modal-pay').classList.remove('open'); });
});