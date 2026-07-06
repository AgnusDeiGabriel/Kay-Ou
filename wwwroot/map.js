// ── Kowòdone apwoksimatif pou chak zòn/komin nan lis "Zòn" fòm ajoute-kay la ──
const HAITI_ZONES = {
  "Acul-du-Nord":                       [19.6833, -72.2667],
  "Anse-à-Pitres":                      [18.0500, -71.7667],
  "Anse-à-Veau":                        [18.5167, -73.3333],
  "Anse-d'Hainault":                    [18.5000, -74.4500],
  "Aquin":                              [18.2833, -73.4000],
  "Arcahaie":                           [18.4189, -72.5261],
  "Bainet":                             [18.1667, -72.7500],
  "Baradères":                          [18.4667, -73.1667],
  "Bombardopolis":                      [19.7000, -73.0167],
  "Cap-Haïtien":                        [19.7592, -72.2014],
  "Carrefour":                          [18.5417, -72.3958],
  "Cayes-Jacmel":                       [18.1961, -72.4808],
  "Cerca-la-Source":                    [19.1667, -71.7333],
  "Chansolme":                          [19.8500, -72.7833],
  "Chardonnières":                      [18.3667, -74.1500],
  "Cornillon":                          [18.4667, -71.9833],
  "Côtes-de-Fer":                       [18.1667, -72.6167],
  "Croix-des-Bouquets":                 [18.5750, -72.2261],
  "Delmas":                             [18.5453, -72.3080],
  "Désarmes":                           [19.0667, -72.4833],
  "Desdunes":                           [19.2500, -72.6000],
  "Dessalines":                         [19.2833, -72.5667],
  "Dondon":                             [19.5667, -72.1667],
  "Ennery":                             [19.4333, -72.5167],
  "Fond-des-Nègres":                    [18.3500, -73.3500],
  "Fort-Liberté":                       [19.6667, -71.8333],
  "Ganthier":                           [18.5556, -72.0972],
  "Gonaïves":                           [19.4500, -72.6833],
  "Grand-Goâve":                        [18.4239, -72.7789],
  "Grande-Rivière-du-Nord":             [19.5978, -72.1067],
  "Grande-Saline":                      [19.2000, -72.7667],
  "Gros-Morne":                         [19.6667, -72.6667],
  "Hinche":                             [19.1500, -72.0167],
  "Jacmel":                             [18.2342, -72.5347],
  "Jérémie":                            [18.6500, -74.1167],
  "Kenscoff":                           [18.4508, -72.2842],
  "La Chapelle":                        [18.8500, -72.2000],
  "Lascahobas":                         [18.8333, -71.9333],
  "Léogâne":                            [18.5119, -72.6339],
  "Les Anglais":                        [18.2833, -74.2333],
  "Les Cayes":                          [18.2000, -73.7500],
  "Les Irois":                          [18.4167, -74.4667],
  "Limbé":                              [19.7000, -72.4000],
  "Limonade":                           [19.7333, -72.1333],
  "Maïssade":                           [19.1667, -72.1167],
  "Marchand-Dessalines":                [19.2833, -72.5667],
  "Marigot":                            [18.2333, -72.3500],
  "Marmelade":                          [19.5167, -72.3833],
  "Miragoâne":                          [18.4453, -73.0917],
  "Mirebalais":                         [18.8333, -72.1000],
  "Môle-Saint-Nicolas":                 [19.8000, -73.3833],
  "Mombin-Crochu":                      [19.3667, -71.8333],
  "Ouanaminthe":                        [19.5500, -71.7333],
  "Pétion-Ville":                       [18.5127, -72.2852],
  "Petite-Rivière-de-l'Artibonite":     [19.1167, -72.4667],
  "Petit-Goâve":                        [18.4272, -72.8672],
  "Plaine-du-Nord":                     [19.6833, -72.2167],
  "Plaisance":                          [19.5667, -72.4667],
  "Port-au-Prince":                     [18.5944, -72.3074],
  "Port-de-Paix":                       [19.9500, -72.8333],
  "Quartier-Morin":                     [19.7167, -72.2167],
  "Roseaux":                            [18.5333, -74.2500],
  "Saint-Louis-du-Nord":                [19.9333, -72.7167],
  "Saint-Louis-du-Sud":                 [18.2833, -73.5500],
  "Saint-Marc":                         [19.1167, -72.7000],
  "Saint-Michel-de-l'Attalaye":         [19.3667, -72.3833],
  "Saint-Raphaël":                      [19.4167, -72.2500],
  "Tabarre":                            [18.5697, -72.2761],
  "Thomazeau":                          [18.6172, -72.1583],
  "Thomonde":                           [19.2333, -71.9333],
  "Torbeck":                            [18.2167, -73.8500],
  "Trou-du-Nord":                       [19.6167, -72.0000],
  "Verrettes":                          [19.0500, -72.4667],
  "Ville-Bonheur":                      [18.9833, -72.1167],
};

// Endèks nòmalize (san aksan, miniskil) pou nou ka matche "Petion-Ville" ak "Pétion-Ville"
function normalizeZoneKey(s) {
  return s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}
const ZONE_LOOKUP = {};
for (const [name, coords] of Object.entries(HAITI_ZONES)) {
  ZONE_LOOKUP[normalizeZoneKey(name)] = coords;
}

function $(id) { return document.getElementById(id); }

// ── State ────────────────────────────────────────────────────────────────
let map, allHouses = [], markers = [];
let curType = '', curZoneQuery = '', curPrixMin = null, curPrixMax = null, curChambreMin = null;

// Limit teritwa a — kwen sid-wès/nò-ès Ayiti (san Kiba, Jamayik, Repiblik Dominikèn)
const HAITI_BOUNDS = L.latLngBounds([17.5, -74.7], [20.2, -71.6]);

async function loadAllHouses() {
  const items = [];
  let page = 1;
  const pageSize = 200;
  while (true) {
    const res = await API.houses.list({ page, pageSize });
    const data = res.data;
    items.push(...data.items);
    if (page >= data.totalPages || data.items.length === 0) break;
    page++;
  }
  return items;
}

function findCoords(zone) {
  if (!zone) return null;
  return ZONE_LOOKUP[normalizeZoneKey(zone)] || null;
}

function fmtCompact(n) {
  return new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(n);
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// ── Filtering ────────────────────────────────────────────────────────────
function filteredHouses() {
  return allHouses.filter(h => {
    if (curType && h.type !== curType) return false;
    if (curZoneQuery && normalizeZoneKey(h.zone) !== normalizeZoneKey(curZoneQuery)) return false;
    if (curPrixMin != null && h.prix < curPrixMin) return false;
    if (curPrixMax != null && h.prix > curPrixMax) return false;
    if (curChambreMin != null && h.chambres < curChambreMin) return false;
    return true;
  });
}

function groupByZone(houses) {
  const groups = {};
  for (const h of houses) {
    const coords = findCoords(h.zone);
    if (!coords) continue;
    const key = normalizeZoneKey(h.zone);
    if (!groups[key]) {
      // Prefere non "kanonik" (jan li ekri nan lis Zòn ofisyèl la) pou parèt konsistan
      const canonical = Object.keys(HAITI_ZONES).find(z => normalizeZoneKey(z) === key) || h.zone;
      groups[key] = { coords, displayName: canonical, houses: [] };
    }
    groups[key].houses.push(h);
  }
  return groups;
}

// ── Markers ──────────────────────────────────────────────────────────────
function buildPinIcon(houses) {
  const hasVann = houses.some(h => h.type === 'Vann');
  const hasLwe = houses.some(h => h.type === 'Lwe');
  const typeCls = hasVann && !hasLwe ? '' : (hasLwe && !hasVann ? ' type-lwe' : '');

  // Non kay la sou pen an lè se yon sèl kay — pi klè pase yon pri (ki ka bay
  // enpresyon se yon pri total achte pandan se yon lwaye chak mwa pou kay Lwe).
  if (houses.length === 1) {
    const label = escapeHtml(houses[0].titre.length > 20 ? houses[0].titre.slice(0, 19) + '…' : houses[0].titre);
    return L.divIcon({
      className: '',
      html: `<div class="zone-pin solo-pin${typeCls}" style="height:30px;"><span title="${escapeHtml(houses[0].titre)}">${label}</span></div>`,
      iconSize: [130, 30],
      iconAnchor: [65, 30],
      popupAnchor: [0, -30]
    });
  }

  const size = Math.min(30 + Math.round(Math.sqrt(houses.length) * 6), 54);
  return L.divIcon({
    className: '',
    html: `<div class="zone-pin${typeCls}" style="width:${size}px;height:${size}px;"><span>${houses.length}</span></div>`,
    iconSize: [size, size],
    iconAnchor: [size / 2, size],
    popupAnchor: [0, -size]
  });
}

function houseThumb(h, cls) {
  const img = h.images && h.images[0] ? `${API.BASE_URL.replace('/api', '')}${h.images[0]}` : null;
  return img
    ? `<img class="${cls}" src="${img}" alt="">`
    : `<div class="${cls}">${h.type === 'Lwe' ? '🏢' : '🏡'}</div>`;
}

function buildPopupHtml(zoneName, houses) {
  const items = houses.map(h => {
    const price = h.type === 'Lwe' ? `$${h.prix.toLocaleString()}/mwa` : `$${h.prix.toLocaleString()}`;
    return `
      <div class="zone-pop-item" data-house-id="${h.id}" onclick="location.href='index.html?house=${h.id}'">
        ${houseThumb(h, 'zone-pop-img')}
        <div class="zone-pop-info">
          <div class="zone-pop-name">${escapeHtml(h.titre)}</div>
          <div class="zone-pop-price">${price}</div>
          <div class="zone-pop-meta">${h.chambres} chanm • ${h.beny} beny • ${h.superficie} m²</div>
        </div>
      </div>`;
  }).join('');

  return `
    <div class="zone-pop-hdr">
      <div class="zone-pop-title">${zoneName}</div>
      <div class="zone-pop-sub">${houses.length} kay disponib</div>
    </div>
    <div class="zone-pop-list">${items}</div>`;
}

function drawMarkers() {
  markers.forEach(m => map.removeLayer(m.marker));
  markers = [];

  const filtered = filteredHouses();
  const groups = groupByZone(filtered);
  const zoneNames = Object.keys(groups);

  $('map-count').textContent = `${filtered.length} kay${curType ? (curType === 'Lwe' ? ' pou lwe' : ' pou vann') : ''}`;
  $('map-empty-banner').classList.toggle('visible', filtered.length > 0 && zoneNames.length === 0);

  for (const zoneKey of zoneNames) {
    const { coords, displayName, houses } = groups[zoneKey];
    const marker = L.marker(coords, { icon: buildPinIcon(houses) })
      .addTo(map)
      .bindPopup(buildPopupHtml(displayName, houses));
    markers.push({ marker, zoneName: displayName, houses });
  }

  if (zoneNames.length > 0) {
    const bounds = L.latLngBounds(zoneNames.map(z => groups[z].coords));
    map.fitBounds(bounds, { padding: [50, 110], maxZoom: 12 });
  }

  updateSheetList();
}

function bounceMarker(marker) {
  const el = marker.getElement();
  if (!el) return;
  const pin = el.querySelector('.zone-pin');
  if (!pin) return;
  pin.classList.remove('pin-bounce');
  void pin.offsetWidth;
  pin.classList.add('pin-bounce');
}

function focusHouseOnMap(houseId) {
  const h = allHouses.find(x => x.id === houseId);
  if (!h) return;
  const entry = markers.find(m => m.houses.some(x => x.id === houseId));
  if (!entry) return;

  $('sheet').classList.remove('expanded');
  map.flyTo(entry.marker.getLatLng(), Math.max(map.getZoom(), 13), { duration: 0.6 });
  entry.marker.openPopup();
  bounceMarker(entry.marker);

  setTimeout(() => {
    document.querySelectorAll('.zone-pop-item').forEach(el => el.classList.remove('zone-pop-item--active'));
    const el = document.querySelector(`.zone-pop-item[data-house-id="${houseId}"]`);
    if (el) { el.classList.add('zone-pop-item--active'); el.scrollIntoView({ block: 'nearest' }); }
  }, 350);
}

// ── Bottom sheet (lis kay ki nan vizyon w) ──────────────────────────────
function sheetItemHtml(h) {
  const price = h.type === 'Lwe' ? `$${h.prix.toLocaleString()}/mwa` : `$${h.prix.toLocaleString()}`;
  return `
    <div class="sheet-item" onclick="focusHouseOnMap(${h.id})">
      ${houseThumb(h, 'sheet-img')}
      <div class="sheet-info">
        <div class="sheet-name">${escapeHtml(h.titre)}</div>
        <div class="sheet-zone">📍 ${h.zone}</div>
        <div class="sheet-price">${price}</div>
        <div class="sheet-meta">${h.chambres} chanm • ${h.beny} beny • ${h.superficie} m²</div>
      </div>
      <svg class="sheet-goto" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 18l6-6-6-6"/></svg>
    </div>`;
}

function updateSheetList() {
  if (!map) return;
  const filtered = filteredHouses();
  const bounds = map.getBounds();
  const visible = filtered.filter(h => {
    const c = findCoords(h.zone);
    return c && bounds.contains(c);
  });

  $('sheet-count').textContent = visible.length === filtered.length
    ? `${visible.length} kay`
    : `${visible.length} kay nan vizyon w (${filtered.length} total)`;

  const list = $('sheet-list');
  if (!visible.length) {
    list.innerHTML = `<div class="sheet-empty">Pa gen kay nan zòn w ap gade a kounye a. Deplase oswa zoome kat la.</div>`;
    return;
  }
  list.innerHTML = visible.map(sheetItemHtml).join('');
}

// ── Sheet drag (peke / ouvri) ────────────────────────────────────────────
function initSheetDrag() {
  const sheet = $('sheet');
  const handle = $('sheet-handle-area');
  let startY = 0, startTranslate = 0, currentTranslate = 0, moved = 0, dragging = false;
  const collapsedTranslate = () => sheet.offsetHeight - 108;

  function getCurrentTranslate() {
    const style = getComputedStyle(sheet);
    const matrix = new DOMMatrixReadOnly(style.transform);
    return matrix.m42;
  }

  function onDown(clientY) {
    dragging = true;
    moved = 0;
    startY = clientY;
    startTranslate = getCurrentTranslate();
    currentTranslate = startTranslate;
    sheet.classList.add('dragging');
  }
  function onMove(clientY) {
    if (!dragging) return;
    const delta = clientY - startY;
    moved = Math.max(moved, Math.abs(delta));
    currentTranslate = Math.max(0, Math.min(collapsedTranslate(), startTranslate + delta));
    sheet.style.transform = `translateY(${currentTranslate}px)`;
  }
  function onUp() {
    if (!dragging) return;
    dragging = false;
    sheet.classList.remove('dragging');
    sheet.style.transform = '';
    if (moved < 6) {
      sheet.classList.toggle('expanded');
    } else {
      sheet.classList.toggle('expanded', currentTranslate < collapsedTranslate() / 2);
    }
  }

  handle.addEventListener('pointerdown', e => onDown(e.clientY));
  window.addEventListener('pointermove', e => onMove(e.clientY));
  window.addEventListener('pointerup', onUp);
}

// ── Filtè tip (chip) ─────────────────────────────────────────────────────
function setTypeFilter(el, type) {
  document.querySelectorAll('.map-chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  curType = type;
  drawMarkers();
}

// ── Rechèch zòn (autocomplete) ───────────────────────────────────────────
function onZoneSearchInput(val) {
  const box = $('zone-suggestions');
  $('zone-search-clear').classList.toggle('visible', !!val);
  if (!val || !val.trim()) { box.innerHTML = ''; box.classList.remove('visible'); return; }

  const norm = normalizeZoneKey(val);
  const matches = Object.keys(HAITI_ZONES).filter(z => normalizeZoneKey(z).includes(norm)).slice(0, 6);

  if (!matches.length) {
    box.innerHTML = `<div class="zone-sugg-empty">Pa gen zòn ki matche</div>`;
  } else {
    box.innerHTML = matches.map(z => `<div class="zone-sugg-item" onclick="selectZone('${z.replace(/'/g, "\\'")}')">${z}</div>`).join('');
  }
  box.classList.add('visible');
}

function selectZone(zoneName) {
  curZoneQuery = zoneName;
  $('zone-search').value = zoneName;
  $('zone-search-clear').classList.add('visible');
  $('zone-suggestions').classList.remove('visible');
  $('zone-active-chip').classList.add('visible');
  $('zone-active-chip-label').textContent = zoneName;
  drawMarkers();
  const coords = HAITI_ZONES[zoneName];
  if (coords) map.flyTo(coords, 12, { duration: 0.6 });
}

function clearZoneQuery() {
  curZoneQuery = '';
  $('zone-search').value = '';
  $('zone-search-clear').classList.remove('visible');
  $('zone-suggestions').classList.remove('visible');
  $('zone-active-chip').classList.remove('visible');
  drawMarkers();
}

// ── Panno filtè (pri / chanm) ─────────────────────────────────────────────
function toggleFilterPanel() {
  const panel = $('filter-panel');
  if (!panel.classList.contains('open')) {
    const hdrRect = document.querySelector('.map-hdr').getBoundingClientRect();
    panel.style.top = (hdrRect.bottom + 10) + 'px';
  }
  panel.classList.toggle('open');
  $('filter-panel-backdrop').classList.toggle('open');
}

function applyFilterPanel() {
  const pMin = parseFloat($('f-prix-min').value);
  const pMax = parseFloat($('f-prix-max').value);
  const cMin = parseInt($('f-chambre-min').value, 10);
  curPrixMin = isNaN(pMin) ? null : pMin;
  curPrixMax = isNaN(pMax) ? null : pMax;
  curChambreMin = isNaN(cMin) ? null : cMin;
  toggleFilterPanel();
  updateFilterBadge();
  drawMarkers();
}

function clearFilters() {
  curPrixMin = curPrixMax = curChambreMin = null;
  $('f-prix-min').value = '';
  $('f-prix-max').value = '';
  $('f-chambre-min').value = '';
  toggleFilterPanel();
  updateFilterBadge();
  drawMarkers();
}

function updateFilterBadge() {
  const active = curPrixMin != null || curPrixMax != null || curChambreMin != null;
  $('map-filter-badge').classList.toggle('visible', active);
  $('map-filter-btn').classList.toggle('has-active', active);
}

// ── Init ─────────────────────────────────────────────────────────────────
async function initMap() {
  map = L.map('map', {
    zoomControl: false,
    maxBounds: HAITI_BOUNDS,
    maxBoundsViscosity: 1.0
  });
  map.fitBounds(HAITI_BOUNDS);
  map.setMinZoom(map.getZoom());
  L.control.zoom({ position: 'bottomright' }).addTo(map);
  L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; OpenStreetMap &copy; CARTO',
    maxZoom: 19,
    minZoom: map.getMinZoom(),
    bounds: HAITI_BOUNDS
  }).addTo(map);

  map.on('moveend zoomend', updateSheetList);

  try {
    allHouses = await loadAllHouses();
  } catch (e) {
    allHouses = [];
  }

  $('map-loading').style.display = 'none';
  drawMarkers();
  initSheetDrag();

  document.addEventListener('click', e => {
    if (!e.target.closest('.map-search-row')) $('zone-suggestions').classList.remove('visible');
  });
}

document.addEventListener('DOMContentLoaded', initMap);
