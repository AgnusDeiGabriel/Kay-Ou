
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
}

const SLIDE_EASE = 'cubic-bezier(0.32, 0.72, 0, 1)';
const SLIDE_DUR  = '0.42s';

function showAuth() {
  showLogin();
  restartSmokeAnims();
  const sp = $('splash');
  // Splash monte anlè sou home a
  Object.assign(sp.style, {
    position: 'fixed', inset: '0', zIndex: '1000',
    display: 'flex', flexDirection: 'column',
    background: 'var(--brown)', height: '100dvh', overflow: 'hidden',
    transform: 'translateY(100%)',
    transition: `transform ${SLIDE_DUR} ${SLIDE_EASE}`
  });
  void sp.offsetWidth;
  sp.style.transform = 'translateY(0)';
  sp.addEventListener('transitionend', () => {
    sp.style.cssText = '';
    goTo('splash');
  }, { once: true });
}

function hideAuth(animated = true) {
  startUnreadPoll();
  if (!animated) {
    goTo('home');
    loadHouses();
    return;
  }
  const sp = $('splash');
  Object.assign(sp.style, {
    position: 'fixed', inset: '0', zIndex: '1000',
    display: 'flex', flexDirection: 'column',
    background: 'var(--brown)', height: '100dvh', overflow: 'hidden',
    transform: 'translateY(0)',
    transition: `transform ${SLIDE_DUR} ${SLIDE_EASE}`
  });
  goTo('home');
  loadHouses();
  void sp.offsetWidth;
  sp.style.transform = 'translateY(100%)';
  sp.addEventListener('transitionend', () => {
    sp.style.cssText = '';
  }, { once: true });
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
      updateHomeUser();
      hideAuth(false);
      return;
    } catch {}
  }
  if (params.get('oauth') === 'error') {
    window.history.replaceState({}, '', '/');
    toast('Koneksyon sosyal echwe. Eseye ankò.', 'err');
  }

  if (API.isLoggedIn()) {
    const user = API.getUser();
    if (!user || !user.id || !user.email) { API.clearAuth(); return; }
    if (API.isAdmin()) { window.location.href = '/admin'; return; }
    updateHomeUser();
    hideAuth(false);
    // hideAuth rele startUnreadPoll() — pa bezwen rele ankò
  }
  // Si pa konekte: splash rete aktif (premye paj)
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
          updateHomeUser();
          hideAuth();
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
    document.querySelectorAll('.social-btn').forEach(btn => {
      const key = btn.classList.contains('social-google')   ? 'google'
                : btn.classList.contains('social-apple')    ? 'apple' : 'facebook';
      btn.disabled     = !active.has(key);
      btn.style.opacity = active.has(key) ? '1' : '0.35';
      btn.title = active.has(key) ? '' : 'Pa konfigire ankò';
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
    if (res.data.user.role === 'Admin') { window.location.href = '/admin'; return; }
    toast(res.message || ('Byenveni ' + res.data.user.nom + ' 🎉'), 'ok', 5000);
    updateHomeUser();
    hideAuth();
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
    hideAuth();
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('btn-login', false);
  }
}

// ── CHAT ─────────────────────────────────────────────────────

// withUserId: non-null sèlman lè pwopriyetè a kap reponn yon vizitè (?with=visitorId)
// otherNom:   non moun k ap chat avèk ou (pou header la)
const ChatState = { houseId: null, ownerId: null, withUserId: null, otherNom: null, fromConv: false, pollTimer: null };
const EditState  = { msgId: null };
const CtxMenu    = { msgId: null, isMine: false, isPinned: false, content: '' };

function openChat() {
  if (!API.isLoggedIn()) { showAuth(); return; }
  const h = State.selectedHouse;
  if (!h) return;
  const me = API.getUser();
  if (me && h.owner && me.id === h.owner.id) {
    toast('Se pwòp kay ou — ou pa ka chat ak ou menm.', 'info');
    return;
  }
  ChatState.houseId  = h.id;
  ChatState.ownerId  = h.owner?.id ?? null;
  ChatState.withUserId = null;   // vizitè — pa bezwen ?with
  ChatState.otherNom   = h.owner?.nom ?? null;
  goTo('chat');
  loadChatMessages();
  startChatPoll();
}

function closeChat() {
  stopChatPoll();
  cancelEditMessage();
  cancelRecording();
  if (_currentAudio) { _currentAudio.pause(); _currentAudio = null; }
  hideCtxMenu();
  hidePinBar();
  const backTo = ChatState.fromConv ? 'conversations' : 'detail';
  ChatState.houseId    = null;
  ChatState.ownerId    = null;
  ChatState.withUserId = null;
  ChatState.otherNom   = null;
  ChatState.fromConv   = false;
  goTo(backTo);
}

function startEditMessage(id, content) {
  EditState.msgId = id;
  const input = $('chat-input');
  input.value = content;
  input.focus();
  input.setSelectionRange(content.length, content.length);
  const bar = $('chat-edit-bar');
  bar.classList.add('active');
  $('chat-edit-preview').textContent = content.length > 50 ? content.slice(0, 50) + '…' : content;
}

function cancelEditMessage() {
  EditState.msgId = null;
  const bar = $('chat-edit-bar');
  if (bar) bar.classList.remove('active');
  const input = $('chat-input');
  if (input) input.value = '';
}

async function loadChatMessages() {
  if (!ChatState.houseId) return;
  try {
    const res = await API.chat.getMessages(ChatState.houseId, ChatState.withUserId);
    if (!res.success) return;
    const { messages, ownerNom, houseTitre, pinnedMessage } = res.data;
    const displayNom = ChatState.otherNom || ownerNom;
    $('chat-owner-av').textContent   = chatInitials(displayNom);
    $('chat-owner-name').textContent = displayNom;
    $('chat-house-name').textContent = houseTitre;
    renderChatMessages(messages);
    if (pinnedMessage) showPinBar(pinnedMessage.content);
    else hidePinBar();
  } catch {}
}

function renderChatMessages(messages) {
  const box = $('chat-messages');
  if (!messages || messages.length === 0) {
    box.innerHTML = `
      <div class="chat-empty">
        <div class="chat-empty-icon">💬</div>
        <div class="chat-empty-txt">Kòmanse konvèsasyon ak pwopriyetè a</div>
      </div>`;
    return;
  }
  box.innerHTML = messages.map(m => {
    const isAudio = !!m.audioUrl;
    const avHtml  = m.senderAvatarUrl
      ? `<img src="${m.senderAvatarUrl}" alt="">`
      : (m.senderNom || '?').split(' ').slice(0,2).map(w => w[0]).join('').toUpperCase();
    const body = isAudio
      ? `<div class="audio-msg">
           <div class="audio-av">${avHtml}</div>
           <button class="audio-play-btn" data-url="${m.audioUrl}" onclick="toggleAudioPlay(this)">${playIconSvg()}</button>
           <div class="audio-wave-wrap">
             <canvas class="audio-wave-canvas" data-url="${m.audioUrl}"></canvas>
             <span class="audio-dur">—</span>
           </div>
         </div>`
      : `<div class="bubble-text">${escHtml(m.content)}</div>`;
    return `
    <div class="chat-bubble ${isAudio ? 'chat-bubble-audio ' : ''}${m.isMine ? 'chat-bubble--mine' : 'chat-bubble--theirs'}${m.isPinned ? ' bubble-pinned' : ''}"
         data-id="${m.id}"
         data-mine="${m.isMine}"
         data-pinned="${m.isPinned}"
         data-audio="${isAudio}"
         data-content="${escHtml(m.content)}"
         oncontextmenu="showMsgMenu(event,this)">
      ${body}
      <div class="bubble-footer">
        ${m.isPinned ? '<span class="bubble-pin-icon">📌</span>' : ''}
        ${m.isEdited ? '<span class="bubble-edited">modifye</span>' : ''}
        <span class="bubble-time">${fmtMsgTime(m.sentAt)}</span>
      </div>
    </div>`;
  }).join('');
  box.scrollTop = box.scrollHeight;
  loadAudioWaveforms();
}

async function sendChatMessage() {
  const input = $('chat-input');
  const text  = input.value.trim();
  if (!text || !ChatState.houseId) return;
  const btn = $('chat-send-btn');
  btn.disabled = true;

  if (EditState.msgId) {
    // Mode modifikasyon
    const msgId = EditState.msgId;
    try {
      await API.chat.editMessage(ChatState.houseId, msgId, text);
      cancelEditMessage();
      await loadChatMessages();
    } catch {
      toast('Modifikasyon pa voye. Eseye ankò.', 'err');
    } finally {
      btn.disabled = false;
      input.focus();
    }
  } else {
    // Mesaj nòmal
    input.value = '';
    try {
      await API.chat.sendMessage(ChatState.houseId, text, ChatState.withUserId);
      await loadChatMessages();
    } catch {
      toast('Mesaj pa voye. Eseye ankò.', 'err');
      input.value = text;
    } finally {
      btn.disabled = false;
      input.focus();
    }
  }
}

// ── CONTEXT MENU ─────────────────────────────────────────────

function showMsgMenu(e, el) {
  e.preventDefault();
  hideCtxMenu();
  CtxMenu.msgId    = +el.dataset.id;
  CtxMenu.isMine   = el.dataset.mine   === 'true';
  CtxMenu.isPinned = el.dataset.pinned === 'true';
  CtxMenu.content  = el.dataset.content;
  const isAudio    =  el.dataset.audio  === 'true';

  const menu = $('msg-ctx-menu');
  let html = '';
  if (CtxMenu.isMine && !isAudio) {
    html += `<div class="ctx-item" onclick="ctxEdit()"><span class="ctx-icon">✏️</span>Modifye</div>`;
    html += `<div class="ctx-sep"></div>`;
  }
  html += `<div class="ctx-item" onclick="ctxPin()"><span class="ctx-icon">📌</span>${CtxMenu.isPinned ? 'Dezepingle' : 'Epingle'}</div>`;
  if (CtxMenu.isMine) {
    html += `<div class="ctx-sep"></div>`;
    html += `<div class="ctx-item ctx-danger" onclick="ctxDelete()"><span class="ctx-icon">🗑️</span>Efase</div>`;
  }
  menu.innerHTML = html;

  // Pozisyon — evite depase bò ekran
  const vw = window.innerWidth, vh = window.innerHeight;
  const mw = 185, mh = menu.childElementCount * 44 + 12;
  let x = e.clientX, y = e.clientY;
  if (x + mw > vw - 8) x = vw - mw - 8;
  if (y + mh > vh - 8) y = vh - mh - 8;
  menu.style.left = x + 'px';
  menu.style.top  = y + 'px';
  menu.classList.add('open');
}

function hideCtxMenu() {
  $('msg-ctx-menu')?.classList.remove('open');
}

function ctxEdit() {
  hideCtxMenu();
  startEditMessage(CtxMenu.msgId, CtxMenu.content);
}

async function ctxDelete() {
  hideCtxMenu();
  if (!ChatState.houseId || !CtxMenu.msgId) return;
  try {
    await API.chat.deleteMessage(ChatState.houseId, CtxMenu.msgId);
    await loadChatMessages();
  } catch {
    toast('Efasman echwe. Eseye ankò.', 'err');
  }
}

async function ctxPin() {
  hideCtxMenu();
  if (!ChatState.houseId || !CtxMenu.msgId) return;
  try {
    const res = await API.chat.pinMessage(ChatState.houseId, CtxMenu.msgId);
    await loadChatMessages();
    toast(res.data?.isPinned ? '📌 Mesaj epingle' : 'Mesaj dezepingle', 'ok', 2000);
  } catch {
    toast('Epinglej echwe. Eseye ankò.', 'err');
  }
}

async function ctxPinFromBar() {
  // Jwenn mesaj epingle a epi dezepingle l
  if (!ChatState.houseId) return;
  try {
    const res = await API.chat.getMessages(ChatState.houseId, ChatState.withUserId);
    const pinned = (res.data?.messages || []).find(m => m.isPinned);
    if (pinned) {
      await API.chat.pinMessage(ChatState.houseId, pinned.id);
      await loadChatMessages();
    }
  } catch {
    toast('Dezepinglej echwe.', 'err');
  }
}

function showPinBar(content) {
  const bar = $('chat-pin-bar');
  if (!bar) return;
  $('chat-pin-text').textContent = content;
  bar.classList.add('active');
}
function hidePinBar() {
  $('chat-pin-bar')?.classList.remove('active');
}

function startChatPoll() {
  stopChatPoll();
  ChatState.pollTimer = setInterval(loadChatMessages, 5000);
}
function stopChatPoll() {
  if (ChatState.pollTimer) { clearInterval(ChatState.pollTimer); ChatState.pollTimer = null; }
}

function chatInitials(name) {
  return (name || '?').split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase();
}
function fmtMsgTime(iso) {
  return new Date(iso).toLocaleTimeString('fr', { hour: '2-digit', minute: '2-digit' });
}
function escHtml(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}

// ── VOICE RECORDING ──────────────────────────────────────────

let _mediaRec     = null;
let _audioChunks  = [];
let _shouldUpload = false;
let _recTimer     = null;
let _recSecs      = 0;
let _currentAudio = null;
let _audioCtx     = null;
let _analyser     = null;
let _waveAnim     = null;

async function toggleRecording() {
  if (_mediaRec && _mediaRec.state === 'recording') {
    _shouldUpload = true;
    _mediaRec.stop();
  } else {
    await startRecording();
  }
}

async function startRecording() {
  if (!navigator.mediaDevices || !window.MediaRecorder) {
    toast('Navigatè ou pa sipòte mikwofòn.', 'err'); return;
  }
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const mime   = ['audio/webm;codecs=opus','audio/webm','audio/ogg','audio/mp4']
                     .find(t => MediaRecorder.isTypeSupported(t)) || '';
    _mediaRec    = new MediaRecorder(stream, mime ? { mimeType: mime } : {});
    _audioChunks = [];
    _shouldUpload = false;

    _mediaRec.ondataavailable = e => { if (e.data.size > 0) _audioChunks.push(e.data); };
    _mediaRec.onstop = async () => {
      stream.getTracks().forEach(t => t.stop());
      setRecUI(false);
      if (_shouldUpload && _audioChunks.length > 0) {
        const type = mime || 'audio/webm';
        const ext  = type.includes('ogg') ? '.ogg' : type.includes('mp4') ? '.mp4' : '.webm';
        const blob = new Blob(_audioChunks, { type });
        const file = new File([blob], `voice${ext}`, { type });
        await uploadVoice(file);
      }
      _audioChunks = [];
    };

    _mediaRec.start(100);
    _recSecs = 0;
    setRecUI(true);
    startWaveform(stream);
    _recTimer = setInterval(() => {
      _recSecs++;
      $('mic-timer').textContent = fmtRecTime(_recSecs);
      if (_recSecs >= 120) { _shouldUpload = true; _mediaRec.stop(); }
    }, 1000);

  } catch {
    toast('Pèmèt aksè mikwofòn nan navigatè ou a.', 'err');
  }
}

function cancelRecording() {
  _shouldUpload = false;
  if (_mediaRec && _mediaRec.state !== 'inactive') _mediaRec.stop();
  else setRecUI(false);
}

function setRecUI(on) {
  clearInterval(_recTimer); _recTimer = null;
  const micBtn = $('mic-btn');
  const recBar = $('mic-record-bar');
  const inRow  = $('chat-input-row');
  if (on) {
    micBtn?.classList.add('recording');
    recBar?.classList.add('active');
    if (inRow) inRow.style.display = 'none';
    $('mic-timer').textContent = '0:00';
  } else {
    micBtn?.classList.remove('recording');
    recBar?.classList.remove('active');
    if (inRow) inRow.style.display = '';
    _mediaRec = null;
    stopWaveform();
  }
}

async function uploadVoice(file) {
  if (!ChatState.houseId) return;
  const btn = $('mic-btn');
  if (btn) btn.disabled = true;
  try {
    await API.chat.sendVoice(ChatState.houseId, file, ChatState.withUserId);
    await loadChatMessages();
  } catch {
    toast('Mesaj vwa pa voye. Eseye ankò.', 'err');
  } finally {
    if (btn) btn.disabled = false;
  }
}

function fmtRecTime(s) {
  return `${Math.floor(s/60)}:${String(s%60).padStart(2,'0')}`;
}

function startWaveform(stream) {
  try {
    _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    _analyser = _audioCtx.createAnalyser();
    _analyser.fftSize = 256;
    _audioCtx.createMediaStreamSource(stream).connect(_analyser);
    setupWaveCanvas();
    drawWave();
  } catch {}
}

function setupWaveCanvas() {
  const canvas = $('mic-wave-canvas');
  if (!canvas) return;
  // Lè bari a vin vizib, li dimansyon reyèl li
  setTimeout(() => {
    const dpr = window.devicePixelRatio || 1;
    const w   = canvas.clientWidth  || 150;
    const h   = canvas.clientHeight || 38;
    canvas.width  = w * dpr;
    canvas.height = h * dpr;
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);
  }, 30);
}

function drawWave() {
  if (!_analyser) return;
  _waveAnim = requestAnimationFrame(drawWave);

  const canvas = $('mic-wave-canvas');
  if (!canvas) return;
  const ctx  = canvas.getContext('2d');
  const dpr  = window.devicePixelRatio || 1;
  const W    = canvas.width  / dpr;
  const H    = canvas.height / dpr;

  const bufLen = _analyser.fftSize;
  const data   = new Uint8Array(bufLen);
  _analyser.getByteTimeDomainData(data);

  ctx.clearRect(0, 0, W, H);

  const barCount = 32;
  const seg      = Math.floor(bufLen / barCount);
  const barW     = W / barCount;
  const gap      = 2.5;

  for (let i = 0; i < barCount; i++) {
    // Amplitude maksimòm nan segman sa
    let peak = 0;
    for (let j = 0; j < seg; j++) {
      const v = Math.abs((data[i * seg + j] - 128) / 128);
      if (v > peak) peak = v;
    }
    const h = Math.max(3, peak * H * 0.88);
    const x = i * barW + gap / 2;
    const bw = barW - gap;
    const y  = (H - h) / 2;
    const r  = Math.min(bw / 2, 3);

    ctx.fillStyle = `rgba(229,57,53,${0.35 + peak * 0.65})`;
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + bw - r, y);
    ctx.arcTo(x + bw, y, x + bw, y + r, r);
    ctx.lineTo(x + bw, y + h - r);
    ctx.arcTo(x + bw, y + h, x + bw - r, y + h, r);
    ctx.lineTo(x + r, y + h);
    ctx.arcTo(x, y + h, x, y + h - r, r);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
    ctx.fill();
  }
}

function stopWaveform() {
  if (_waveAnim) { cancelAnimationFrame(_waveAnim); _waveAnim = null; }
  _analyser = null;
  if (_audioCtx) { _audioCtx.close().catch(() => {}); _audioCtx = null; }
  const canvas = $('mic-wave-canvas');
  if (canvas) canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
}

// ── AUDIO WAVEFORM (player) ───────────────────────────────────

const _waveCache = {};  // url → Float32Array amplitudes

async function loadWaveformData(url, barCount = 50) {
  if (_waveCache[url]) return _waveCache[url];
  const resp   = await fetch(url);
  const buf    = await resp.arrayBuffer();
  const tmpCtx = new (window.AudioContext || window.webkitAudioContext)();
  const audio  = await tmpCtx.decodeAudioData(buf);
  tmpCtx.close();

  const ch      = audio.getChannelData(0);
  const seg     = Math.floor(ch.length / barCount);
  const amps    = new Float32Array(barCount);
  for (let i = 0; i < barCount; i++) {
    let peak = 0;
    for (let j = 0; j < seg; j++) {
      const v = Math.abs(ch[i * seg + j]);
      if (v > peak) peak = v;
    }
    amps[i] = peak;
  }
  // Normalise
  const max = Math.max(...amps, 0.01);
  for (let i = 0; i < barCount; i++) amps[i] /= max;

  _waveCache[url] = amps;
  return amps;
}

function setupAudioCanvas(canvas) {
  return new Promise(resolve => requestAnimationFrame(() => {
    const dpr = window.devicePixelRatio || 1;
    const w   = canvas.clientWidth  || 180;
    const h   = canvas.clientHeight || 28;
    canvas.width  = w * dpr;
    canvas.height = h * dpr;
    canvas.getContext('2d').scale(dpr, dpr);
    resolve();
  }));
}

function drawStaticWave(canvas, amps, progress = 0) {
  if (!canvas || !amps) return;
  const dpr  = window.devicePixelRatio || 1;
  const W    = canvas.width  / dpr;
  const H    = canvas.height / dpr;
  const ctx  = canvas.getContext('2d');
  const mine = canvas.closest('.chat-bubble--mine') !== null;

  const playedCol   = mine ? 'rgba(255,255,255,.95)' : 'rgba(212,83,26,.90)';
  const unplayedCol = mine ? 'rgba(255,255,255,.28)' : 'rgba(212,83,26,.22)';

  ctx.clearRect(0, 0, W, H);

  const n      = amps.length;
  const bW     = W / n;
  const gap    = 1.2;
  const played = Math.round(progress * n);

  for (let i = 0; i < n; i++) {
    const amp  = amps[i];
    const h    = Math.max(3, amp * H * 0.88);
    const x    = i * bW + gap / 2;
    const bw   = bW - gap;
    const y    = (H - h) / 2;
    const r    = Math.min(bw / 2, 2.5);

    ctx.fillStyle = i < played ? playedCol : unplayedCol;
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + bw - r, y);
    ctx.arcTo(x + bw, y, x + bw, y + r, r);
    ctx.lineTo(x + bw, y + h - r);
    ctx.arcTo(x + bw, y + h, x + bw - r, y + h, r);
    ctx.lineTo(x + r, y + h);
    ctx.arcTo(x, y + h, x, y + h - r, r);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
    ctx.fill();
  }
}

async function loadAudioWaveforms() {
  const canvases = document.querySelectorAll('.audio-wave-canvas[data-url]');
  for (const canvas of canvases) {
    const url = canvas.dataset.url;
    await setupAudioCanvas(canvas);
    try {
      const amps = await loadWaveformData(url);
      canvas._amps = amps;
      drawStaticWave(canvas, amps, 0);
    } catch {
      // Fallback: ba plat si echwe
      const flat = new Float32Array(40).fill(0.25);
      canvas._amps = flat;
      drawStaticWave(canvas, flat, 0);
    }
  }
}

// ── AUDIO PLAYBACK ────────────────────────────────────────────

function toggleAudioPlay(btn) {
  const url    = btn.dataset.url;
  const msgEl  = btn.closest('.audio-msg');
  const canvas = msgEl?.querySelector('.audio-wave-canvas');
  const durEl  = msgEl?.querySelector('.audio-dur');

  if (_currentAudio) {
    if (_currentAudio._url === url && !_currentAudio.paused) {
      _currentAudio.pause();
      btn.innerHTML = playIconSvg();
      return;
    }
    _currentAudio.pause();
    document.querySelectorAll('.audio-play-btn').forEach(b => { b.innerHTML = playIconSvg(); });
  }

  const audio   = new Audio(url);
  audio._url    = url;
  _currentAudio = audio;

  audio.onloadedmetadata = () => {
    if (durEl && isFinite(audio.duration))
      durEl.textContent = fmtRecTime(Math.floor(audio.duration));
  };
  audio.ontimeupdate = () => {
    const prog = audio.duration ? audio.currentTime / audio.duration : 0;
    if (canvas?._amps) drawStaticWave(canvas, canvas._amps, prog);
    if (durEl) durEl.textContent = fmtRecTime(Math.floor(audio.currentTime));
  };
  audio.onended = () => {
    btn.innerHTML = playIconSvg();
    if (canvas?._amps) drawStaticWave(canvas, canvas._amps, 0);
    if (durEl && isFinite(audio.duration))
      durEl.textContent = fmtRecTime(Math.floor(audio.duration));
  };

  audio.play().catch(() => toast('Pa ka jwe son an.', 'err'));
  btn.innerHTML = pauseIconSvg();
}

function playIconSvg() {
  return `<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M8 5v14l11-7z"/></svg>`;
}
function pauseIconSvg() {
  return `<svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"/></svg>`;
}

// ── CONVERSATIONS ────────────────────────────────────────────

let _unreadPollTimer = null;

async function openConversations() {
  if (!API.isLoggedIn()) { showAuth(); return; }
  goTo('conversations');
  $('conv-list').innerHTML = '<div class="loader"><div class="spinner"></div></div>';
  try {
    const res = await API.chat.conversations();
    renderConversations(res.data || []);
  } catch {
    $('conv-list').innerHTML = '<div class="empty-state"><div class="empty-icon">⚠️</div><div class="empty-title">Erè chajman</div></div>';
  }
}

function renderConversations(list) {
  if (!list.length) {
    $('conv-list').innerHTML = `
      <div class="empty-state">
        <div class="empty-icon">💬</div>
        <div class="empty-title">Pa gen mesaj</div>
        <div class="empty-sub">Konvèsasyon ou yo ap parèt isit.</div>
      </div>`;
    return;
  }

  $('conv-list').innerHTML = list.map(c => {
    const initials  = c.otherUserNom.split(' ').slice(0,2).map(w=>w[0]).join('').toUpperCase();
    const timeStr   = fmtConvTime(c.lastMessageAt);
    const hasUnread = c.unreadCount > 0;
    const avatarHtml = c.otherUserAvatarUrl
      ? `<img src="${c.otherUserAvatarUrl}" alt="${escHtml(c.otherUserNom)}">`
      : initials;
    const preview = c.lastMessage || '🎵 Mesaj vwa';
    const prefix = c.isLastMessageMine ? '<span class="conv-sent-prefix">Mwen: </span>' : '';

    return `
      <div class="conv-item"
           data-hid="${c.houseId}"
           data-uid="${c.otherUserId}"
           data-nom="${escHtml(c.otherUserNom)}"
           data-owner="${c.isCurrentUserOwner ? 'true' : 'false'}"
           onclick="openChatFromConv(this)">
        <div class="conv-av">${avatarHtml}</div>
        <div class="conv-body">
          <div class="conv-top">
            <span class="conv-name">${escHtml(c.otherUserNom)}</span>
            <span class="conv-time">${timeStr}</span>
          </div>
          <div class="conv-bot">
            <span class="conv-preview${hasUnread ? ' unread' : ''}">${prefix}${escHtml(preview)}</span>
            ${hasUnread ? `<span class="conv-unread-badge">${c.unreadCount}</span>` : ''}
          </div>
          <div class="conv-house">🏠 ${escHtml(c.houseTitre)}</div>
        </div>
      </div>`;
  }).join('');
}

function openChatFromConv(el) {
  openChatFromConversation(
    +el.dataset.hid,
    +el.dataset.uid,
    el.dataset.nom,
    el.dataset.owner === 'true'
  );
}

function openChatFromConversation(houseId, otherUserId, otherNom, isOwner) {
  ChatState.houseId    = houseId;
  ChatState.ownerId    = isOwner ? (API.getUser()?.id ?? null) : otherUserId;
  ChatState.withUserId = isOwner ? otherUserId : null;
  ChatState.otherNom   = otherNom || null;
  ChatState.fromConv   = true;
  goTo('chat');
  loadChatMessages();
  startChatPoll();
}

function fmtConvTime(iso) {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = now - d;
  const diffDays = Math.floor(diffMs / 86400000);
  if (diffDays === 0) return d.toLocaleTimeString('fr', { hour:'2-digit', minute:'2-digit' });
  if (diffDays === 1) return 'Yè';
  if (diffDays < 7)  return diffDays + 'j';
  return d.toLocaleDateString('fr', { day:'numeric', month:'short' });
}

async function updateUnreadBadge() {
  if (!API.isLoggedIn()) return;
  try {
    const res = await API.chat.conversations();
    const total = (res.data || []).reduce((sum, c) => sum + (c.unreadCount || 0), 0);
    const badge = $('msg-badge');
    if (!badge) return;
    if (total > 0) {
      badge.textContent = total > 99 ? '99+' : total;
      badge.classList.add('visible');
    } else {
      badge.classList.remove('visible');
    }
  } catch {}
}

function startUnreadPoll() {
  stopUnreadPoll();
  updateUnreadBadge();
  _unreadPollTimer = setInterval(updateUnreadBadge, 30000);
}
function stopUnreadPoll() {
  if (_unreadPollTimer) { clearInterval(_unreadPollTimer); _unreadPollTimer = null; }
}

// ─────────────────────────────────────────────────────────────

async function doLogout() {
  try {
    await API.auth.logout(API.getRefresh());
  } catch {}
  API.clearAuth();
  stopChatPoll();
  stopUnreadPoll();
  $('msg-badge')?.classList.remove('visible');
  State.houses = [];
  goTo('home');
  loadHouses();
  showAuth(); // splash monte sou home
}

function updateHomeUser() {
  const user = API.getUser();
  if (!user) return;
  const ini = user.nom.split(' ').map(n => n[0]).join('').toUpperCase().slice(0,2);
  if (user.avatarUrl) {
    $('home-avatar').innerHTML = `<img src="${user.avatarUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:50%;">`;
  } else {
    $('home-avatar').textContent = ini;
  }
  renderAvatarEl($('profile-av'), user.avatarUrl, user.nom);
  $('profile-name').textContent  = user.nom;
  $('profile-email').textContent = user.email;
}

async function loadProfileStats() {
  try {
    const res  = await API.auth.me();
    const user = res.data;
    // Mete ajou localStorage avèk done frèch
    API.saveAuth({ accessToken: API.getToken(), refreshToken: API.getRefresh(), user });
    updateHomeUser();
    // Stats
    $('pf-stat-houses').textContent   = user.houseCount   ?? 0;
    $('pf-stat-payments').textContent = user.paymentCount ?? 0;
    const yr = new Date(user.createdAt).getFullYear();
    $('pf-stat-since').textContent    = yr;
    // Badge verifye (kache si pa verifye — pa gen champ nan DTO, montre pa defò)
  } catch {}
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
    const imgSrc = h.images && h.images[0]
      ? `${API.BASE_URL.replace('/api','')}${h.images[0]}`
      : null;
    const priceFmt = h.type === 'Lwe'
      ? `$${h.prix.toLocaleString()} <span>/mwa</span>`
      : `$${h.prix.toLocaleString()} <span>USD</span>`;
    const bg = h.type === 'Lwe'
      ? 'linear-gradient(135deg,#D4E8C9,#9FC98A)'
      : 'linear-gradient(135deg,#E8D5C4,#C9A98A)';

    return `
      <div class="house-card" onclick="openDetail(${h.id})">
        <div class="card-img" style="background:${bg}">
          ${imgSrc
            ? `<img src="${imgSrc}" alt="${h.titre}" loading="lazy">`
            : `<div class="card-img-placeholder">${h.type === 'Lwe' ? '🏢' : '🏡'}</div>`}
          <span class="card-badge badge-${h.type.toLowerCase()}">${h.type === 'Vann' ? 'Pou Vann' : 'Pou Lwe'}</span>
          ${h.statut === 'Approved' ? '<span class="badge-verified">✓ Verifye</span>' : ''}
          <div class="card-overlay">
            <div class="card-title">${h.titre}</div>
            <div class="card-zone-price">
              <span class="card-zone">📍 ${h.zone}</span>
              <span class="card-price">${priceFmt}</span>
            </div>
          </div>
        </div>
        <div class="card-stats-row">
          <div class="card-stat-item"><b>${h.chambres}</b>Chanm</div>
          <div class="card-stat-item"><b>${h.beny}</b>Beny</div>
          <div class="card-stat-item"><b>${h.superficie}</b>m²</div>
        </div>
        <div class="card-actions-row">
          <button class="card-like-btn${h.isLikedByCurrentUser ? ' liked' : ''}"
            data-id="${h.id}" data-liked="${h.isLikedByCurrentUser}" data-count="${h.likeCount}"
            onclick="toggleLike(event,this)" title="Like">
            <svg viewBox="0 0 24 24" fill="${h.isLikedByCurrentUser ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2">
              <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
            </svg>
            <span>${h.likeCount}</span>
          </button>
          <button class="card-share-btn" data-id="${h.id}" onclick="shareHouse(event,this)" title="Pataje">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
              <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
            </svg>
          </button>
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

function navClick(el, action) {
  document.querySelectorAll('.nav-btn[data-nav]').forEach(b => b.classList.remove('active'));
  el.classList.add('active');
  const tabs = document.querySelectorAll('.tab');
  if (action === 'home') {
    goTo('home');
    State.curTab = 'all';
    tabs.forEach((t, i) => t.classList.toggle('active', i === 0));
    loadHouses();
  } else if (action === 'vann') {
    goTo('home');
    State.curTab = 'Vann';
    tabs.forEach((t, i) => t.classList.toggle('active', i === 1));
    loadHouses();
  } else if (action === 'lwe') {
    goTo('home');
    State.curTab = 'Lwe';
    tabs.forEach((t, i) => t.classList.toggle('active', i === 2));
    loadHouses();
  } else if (action === 'profile') {
    goTo('profile');
    loadProfileStats();
  }
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
  if (!API.isLoggedIn()) { showAuth(); return; }

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

function handlePayCta() {
  const h = State.selectedHouse;
  if (!h) return;
  if (h.userHasPaid && h.owner?.telefon) {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(h.owner.telefon)
        .then(() => toast('Nimewo kopye: ' + h.owner.telefon, 'ok', 4000))
        .catch(() => toast('📞 ' + h.owner.telefon, 'ok', 6000));
    } else {
      toast('📞 ' + h.owner.telefon, 'ok', 6000);
    }
    return;
  }
  showPayment();
}

function showPayment() {
  if (!API.isLoggedIn()) { showAuth(); return; }
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
  const telefon       = $('pay-tel').value.trim();
  const methodEl      = document.querySelector('.pay-method.sel');
  const paymentMethod = methodEl ? methodEl.querySelector('.pay-method-name')?.textContent : 'MonCash';

  setLoading('btn-submit-pay', true);
  try {
    await API.payments.pay(h.id, State.resiFile, telefon, paymentMethod);
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


async function openMyHouses() {
  goTo('my-houses');
  const list = $('my-houses-list');
  list.innerHTML = '<div class="loader"><div class="spinner"></div></div>';
  try {
    const res = await API.houses.my();
    const houses = res.data;
    if (!houses.length) {
      list.innerHTML = `
        <div class="empty-state" style="padding:60px 20px;text-align:center;">
          <div style="font-size:44px;">🏠</div>
          <div style="margin-top:12px;font-size:14px;color:var(--muted);">Ou poko poste okenn kay.</div>
        </div>`;
      return;
    }
    list.innerHTML = houses.map(h => {
      const img = h.images?.[0]?.imageUrl
        ? `<img src="${h.images[0].imageUrl}" class="mh-img">`
        : `<div class="mh-img mh-img--placeholder">🏠</div>`;
      const statusClass = { Pending:'pending', Approved:'approved', Rejected:'rejected' }[h.statut] ?? 'pending';
      const statusLabel = { Pending:'⏳ Annatant', Approved:'✅ Apwouve', Rejected:'❌ Rejte' }[h.statut] ?? h.statut;
      return `
        <div class="mh-card" onclick="openDetail(${h.id})">
          ${img}
          <div class="mh-body">
            <div class="mh-top">
              <div class="mh-title">${h.titre}</div>
              <span class="pill pill-${statusClass}">${statusLabel}</span>
            </div>
            <div class="mh-zone">📍 ${h.zone}</div>
            <div class="mh-price">${h.prix.toLocaleString()} HTG${h.type === 'Lwe' ? ' / mwa' : ''}</div>
            ${h.statut === 'Rejected' && h.rejectionReason ? `<div class="mh-reason">⚠️ ${h.rejectionReason}</div>` : ''}
          </div>
        </div>`;
    }).join('');
  } catch (e) {
    list.innerHTML = `<div class="empty-state"><div class="empty-sub">Erè: ${e.message}</div></div>`;
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

// ── EDIT PROFILE ─────────────────────────────────────────────────────────────

// Affiche foto oswa inityal nan yon elèman .ep-avatar
function renderAvatarEl(el, avatarUrl, nom) {
  if (avatarUrl) {
    el.innerHTML = `<img src="${avatarUrl}" alt="${nom}">`;
  } else {
    const ini = (nom || '?').split(' ').slice(0,2).map(w => w[0]).join('').toUpperCase();
    el.textContent = ini;
  }
}

function openEditProfile() {
  const user = API.getUser();
  if (!user) return;
  renderAvatarEl($('ep-avatar'), user.avatarUrl, user.nom);
  $('ep-nom').value        = user.nom     || '';
  $('ep-tel').value        = user.telefon || '';
  $('ep-email').value      = user.email   || '';
  $('ep-old-pw').value     = '';
  $('ep-new-pw').value     = '';
  $('ep-confirm-pw').value = '';
  goTo('edit-profile');
}

async function onAvatarFileChange(input) {
  const file = input.files[0];
  if (!file) return;
  // Preview imedya
  const reader = new FileReader();
  reader.onload = e => { $('ep-avatar').innerHTML = `<img src="${e.target.result}" alt="avatar">`; };
  reader.readAsDataURL(file);
  // Upload
  try {
    const res = await API.auth.uploadAvatar(file);
    const user = API.getUser();
    const newAvatarUrl = res.data.avatarUrl;
    API.saveAuth({ accessToken: API.getToken(), refreshToken: API.getRefresh(), user: { ...user, avatarUrl: newAvatarUrl } });
    updateHomeUser();
    renderAvatarEl($('profile-av'), newAvatarUrl, user.nom);
    toast('Foto pwofil ajou!', 'ok');
  } catch (e) {
    toast(e.message, 'err');
    // Reverifikasyon — remete foto ansyen
    const user = API.getUser();
    renderAvatarEl($('ep-avatar'), user.avatarUrl, user.nom);
  }
  input.value = '';
}

async function saveProfile() {
  const nom    = $('ep-nom').value.trim();
  const telefon= $('ep-tel').value.trim();
  if (!nom)    { toast('Non obligatwa.', 'err'); return; }
  if (!telefon){ toast('Telefòn obligatwa.', 'err'); return; }

  setLoading('ep-save-btn', true);
  try {
    const res  = await API.auth.updateProfile({ nom, telefon });
    const user = API.getUser();
    // Mete ajou localStorage (konsève tout lòt champ)
    API.saveAuth({ accessToken: API.getToken(), refreshToken: API.getRefresh(), user: { ...user, nom, telefon } });
    updateHomeUser();
    renderAvatarEl($('ep-avatar'), user.avatarUrl, nom);
    toast(res.message || 'Pwofil ajou!', 'ok');
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('ep-save-btn', false);
  }
}

async function changePassword() {
  const oldPw  = $('ep-old-pw').value;
  const newPw  = $('ep-new-pw').value;
  const confPw = $('ep-confirm-pw').value;

  if (!oldPw)           { toast('Mete ansyen modpas ou.', 'err'); return; }
  if (newPw.length < 6) { toast('Nouvo modpas dwe gen omwen 6 karaktè.', 'err'); return; }
  if (newPw !== confPw) { toast('Modpas konfirmasyon an pa menm.', 'err'); return; }

  setLoading('ep-pw-btn', true);
  try {
    const res = await API.auth.changePassword({ oldPassword: oldPw, newPassword: newPw });
    $('ep-old-pw').value     = '';
    $('ep-new-pw').value     = '';
    $('ep-confirm-pw').value = '';
    toast(res.message || 'Modpas chanje!', 'ok');
  } catch (e) {
    toast(e.message, 'err');
  } finally {
    setLoading('ep-pw-btn', false);
  }
}

// ── LIKE / SHARE ──────────────────────────────────────────────────────────────

async function toggleLike(e, btn) {
  e.stopPropagation();
  if (!API.isLoggedIn()) { showAuth(); return; }

  const houseId  = +btn.dataset.id;
  const wasLiked = btn.dataset.liked === 'true';
  const count    = +btn.dataset.count;
  const newCount = wasLiked ? count - 1 : count + 1;

  // Optimistic update
  btn.dataset.liked = String(!wasLiked);
  btn.dataset.count = String(newCount);
  btn.classList.toggle('liked', !wasLiked);
  btn.querySelector('svg').setAttribute('fill', !wasLiked ? 'currentColor' : 'none');
  btn.querySelector('span').textContent = newCount;

  try {
    const res = await API.houses.toggleLike(houseId);
    btn.dataset.liked = String(res.data.isLiked);
    btn.dataset.count = String(res.data.likeCount);
    btn.classList.toggle('liked', res.data.isLiked);
    btn.querySelector('svg').setAttribute('fill', res.data.isLiked ? 'currentColor' : 'none');
    btn.querySelector('span').textContent = res.data.likeCount;
  } catch (err) {
    btn.dataset.liked = String(wasLiked);
    btn.dataset.count = String(count);
    btn.classList.toggle('liked', wasLiked);
    btn.querySelector('svg').setAttribute('fill', wasLiked ? 'currentColor' : 'none');
    btn.querySelector('span').textContent = count;
    toast(err.message, 'err');
  }
}

async function shareHouse(e, btn) {
  e.stopPropagation();
  const houseId = +btn.dataset.id;
  const h = State.houses.find(x => x.id === houseId);
  const title = h ? `${h.titre} — ${h.zone}` : 'KayOu';
  const url   = `${window.location.origin}/?house=${houseId}`;

  if (navigator.share) {
    try { await navigator.share({ title, url }); } catch {}
  } else {
    try { await navigator.clipboard.writeText(url); toast('Lyen kopye!', 'ok'); }
    catch { toast(url, 'info', 5000); }
  }
}

document.addEventListener('DOMContentLoaded', () => {
  if (!API.isLoggedIn()) {
    const sp = $('splash');
    sp.classList.add('monte');
    sp.addEventListener('animationend', () => sp.classList.remove('monte'), { once: true });
  }
  initSplash();
  initGoogleSignIn();
  loadSocialProviders();

  // Fèmen menu kontèks lè klike nenpòt kote
  document.addEventListener('click', () => hideCtxMenu());
  document.addEventListener('keydown', e => { if (e.key === 'Escape') hideCtxMenu(); });
});