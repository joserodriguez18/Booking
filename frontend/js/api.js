const API_BASE = 'http://localhost:8081/api';

// ── Autenticación ─────────────────────────────────────────────────────────────

const auth = {
    token: () => localStorage.getItem('bk_token'),
    user: () => JSON.parse(localStorage.getItem('bk_user') || 'null'),
    isLogged: () => !!localStorage.getItem('bk_token'),

    save(data) {
        localStorage.setItem('bk_token', data.accessToken);
        localStorage.setItem('bk_refresh', data.refreshToken);
        const p = JSON.parse(atob(data.accessToken.split('.')[1]));
        localStorage.setItem('bk_user', JSON.stringify({
            id: p.sub, name: p.name, email: p.email, role: p.role, kycStatus: p.kyc_status
        }));
    },

    logout() {
        ['bk_token', 'bk_refresh', 'bk_user'].forEach(k => localStorage.removeItem(k));
        window.location.href = 'index.html';
    }
};

// ── Fetch base ────────────────────────────────────────────────────────────────

async function req(url, opts = {}) {
    const headers = {};
    const token = auth.token();
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (!(opts.body instanceof FormData)) headers['Content-Type'] = 'application/json';

    const res = await fetch(API_BASE + url, {...opts, headers: {...headers, ...opts.headers}});

    if (res.status === 204) return null;
    const text = await res.text();
    const data = text ? JSON.parse(text) : null;
    if (!res.ok) throw new Error(data?.message || data?.title || data?.error || `Error ${res.status}`);
    return data;
}

// ── API ───────────────────────────────────────────────────────────────────────

const api = {
    login: (email, pass) => req('/auth/login', {method: 'POST', body: JSON.stringify({email, password: pass})}),
    register: (name, email, pass, role = 'Guest') => req('/auth/register', {
        method: 'POST',
        body: JSON.stringify({name, email, password: pass, role})
    }),
    refresh: () => req('/auth/refresh', {
        method: 'POST',
        body: JSON.stringify({refreshToken: localStorage.getItem('bk_refresh')})
    }),

    getProperties: (params = {}) => req('/properties?' + new URLSearchParams(
        Object.fromEntries(Object.entries(params).filter(([, v]) => v)))),
    getProperty: (id) => req(`/properties/${id}`),
    createProperty: (data) => req('/properties', {method: 'POST', body: JSON.stringify(data)}),
    updateProperty: (id, data) => req(`/properties/${id}`, {method: 'PUT', body: JSON.stringify(data)}),
    uploadPhoto: (id, fd) => fetch(API_BASE + `/properties/${id}/photos`, {
        method: 'POST',
        headers: {Authorization: `Bearer ${auth.token()}`},
        body: fd
    }).then(r => r.json()),

    createBooking: (data) => req('/bookings', {method: 'POST', body: JSON.stringify(data)}),
    confirmBooking: (id) => req(`/bookings/${id}/confirm`, {method: 'POST'}),
    cancelBooking: (id) => req(`/bookings/${id}/cancel`, {method: 'POST'}),
    getMyBookings: () => req('/bookings/my'),

    getWishlist: () => req('/wishlist'),
    addWishlist: (id) => req(`/wishlist/${id}`, {method: 'POST'}),
    removeWishlist: (id) => req(`/wishlist/${id}`, {method: 'DELETE'}),

    uploadKyc: (fd) => fetch(API_BASE + '/kyc/upload', {
        method: 'POST',
        headers: {Authorization: `Bearer ${auth.token()}`},
        body: fd
    }).then(async r => {
        const d = await r.json();
        if (!r.ok) throw new Error(d?.error || d?.message || 'Error en verificación');
        return d;
    }),

    getDashboard: (params = {}) => req('/owner/dashboard?' + new URLSearchParams(params)),
    exportReport: (propId) => fetch(API_BASE + '/owner/report/export' + (propId ? `?propertyId=${propId}` : ''), {
        headers: {Authorization: `Bearer ${auth.token()}`}
    }),
};

// ── Utilidades UI ─────────────────────────────────────────────────────────────

function toast(msg, tipo = 'ok') {
    const el = document.createElement('div');
    el.className = `fixed bottom-5 right-5 z-50 px-5 py-3 rounded-xl text-white text-sm shadow-lg
    ${tipo === 'error' ? 'bg-red-500' : 'bg-emerald-500'}`;
    el.textContent = msg;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 3500);
}

function formatPrecio(amount, currency = 'COP') {
    return new Intl.NumberFormat('es-CO', {
        style: 'currency', currency, maximumFractionDigits: 0
    }).format(amount);
}

function formatFecha(str) {
    if (!str) return '';
    return new Date(str).toLocaleDateString('es-CO', {day: '2-digit', month: 'short', year: 'numeric'});
}

function diffNoches(ci, co) {
    return Math.max(1, Math.round((new Date(co) - new Date(ci)) / 86400000));
}

function statusBadge(s) {
    const m = {
        Pending: ['bg-amber-100 text-amber-700', 'Pendiente'],
        Confirmed: ['bg-emerald-100 text-emerald-700', 'Confirmada'],
        Cancelled: ['bg-red-100 text-red-600', 'Cancelada']
    };
    const [cls, label] = m[s] || ['bg-gray-100 text-gray-600', s];
    return `<span class="px-2 py-0.5 rounded-full text-xs font-medium ${cls}">${label}</span>`;
}

function kycBadge(s) {
    const m = {
        NotStarted: ['bg-gray-100 text-gray-600', 'Sin verificar'],
        Pending: ['bg-amber-100 text-amber-700', 'En revisión'],
        Approved: ['bg-emerald-100 text-emerald-700', 'Verificado ✓'],
        Rejected: ['bg-red-100 text-red-600', 'Rechazado']
    };
    const [cls, label] = m[s] || ['bg-gray-100 text-gray-600', s || 'Desconocido'];
    return `<span class="px-3 py-1 rounded-full text-xs font-medium ${cls}">${label}</span>`;
}

const PLACEHOLDER = `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="400" height="260"><rect fill="%23f3f4f6" width="400" height="260"/><text fill="%23d1d5db" x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" font-size="60">🏠</text></svg>`;

function renderNav() {
    const el = document.getElementById('nav-user');
    if (!el) return;
    const u = auth.user();
    if (u) {
        const esOwner = u.role === 'Owner';
        el.innerHTML = `
      <a href="profile.html" class="text-sm text-gray-600 hover:text-rose-500 flex items-center gap-1">
        <span class="w-7 h-7 rounded-full bg-rose-100 flex items-center justify-center text-xs font-bold text-rose-600">${u.name[0].toUpperCase()}</span>
        <span class="hidden sm:inline">${u.name.split(' ')[0]}</span>
      </a>
      ${esOwner ? '<a href="dashboard.html" class="text-sm text-gray-600 hover:text-rose-500 hidden sm:inline">📊 Panel</a>' : ''}
      <button onclick="auth.logout()" class="text-sm px-3 py-1.5 border border-gray-200 rounded-full hover:border-rose-300 hover:text-rose-500 transition-colors">Salir</button>`;
    } else {
        el.innerHTML = `
      <a href="auth.html" class="text-sm text-gray-600 hover:text-rose-500">Iniciar sesión</a>
      <a href="auth.html?r=1" class="text-sm px-4 py-1.5 bg-rose-500 hover:bg-rose-600 text-white rounded-full transition-colors">Registrarse</a>`;
    }
}
