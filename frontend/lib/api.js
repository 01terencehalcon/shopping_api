// API Service for Clothes Shopping System
const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:43205';

// Default mock products matching database seed data
export const FALLBACK_PRODUCTS = [
  {
    id: 1,
    name: "Classic White Tee",
    category: "Tops",
    price: 29.0,
    stock: 142,
    image: "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80",
    description: "A wardrobe essential in premium 100% cotton. Features a clean crew neck, reinforced shoulder stitching, and relaxed fit."
  },
  {
    id: 2,
    name: "Slim Chino Pants",
    category: "Bottoms",
    price: 59.0,
    stock: 58,
    image: "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?auto=format&fit=crop&w=600&q=80",
    description: "Tailored slim-fit chinos crafted with stretch cotton twill for all-day flexibility and sleek style."
  },
  {
    id: 3,
    name: "Canvas Sneakers",
    category: "Shoes",
    price: 89.0,
    stock: 35,
    image: "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?auto=format&fit=crop&w=600&q=80",
    description: "Timeless low-top canvas sneakers featuring vulcanized rubber sole and padded insole for supreme comfort."
  },
  {
    id: 4,
    name: "Leather Crossbody",
    category: "Accessories",
    price: 119.0,
    stock: 23,
    image: "https://images.unsplash.com/photo-1548036328-c9fa89d128fa?auto=format&fit=crop&w=600&q=80",
    description: "Genuine full-grain leather crossbody bag with polished hardware and adjustable strap."
  },
  {
    id: 5,
    name: "Linen Button Shirt",
    category: "Tops",
    price: 45.0,
    stock: 42,
    image: "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?auto=format&fit=crop&w=600&q=80",
    description: "Breathable pure linen button-up shirt with airy weave, ideal for warm climates and relaxed layering."
  },
  {
    id: 6,
    name: "High-Rise Jeans",
    category: "Bottoms",
    price: 75.0,
    stock: 94,
    image: "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?auto=format&fit=crop&w=600&q=80",
    description: "Classic straight-leg denim designed with a flattering high waistline and vintage medium wash."
  },
  {
    id: 7,
    name: "Knit Cardigan",
    category: "Tops",
    price: 65.0,
    stock: 31,
    image: "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?auto=format&fit=crop&w=600&q=80",
    description: "Cozy ribbed cardigan sweater with tortoise-shell buttons and dropped shoulders."
  },
  {
    id: 8,
    name: "Running Shorts",
    category: "Bottoms",
    price: 35.0,
    stock: 50,
    image: "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?auto=format&fit=crop&w=600&q=80",
    description: "Ultra-lightweight performance shorts with moisture-wicking liner and zipped key pocket."
  },
  {
    id: 9,
    name: "Satin Slip Dress",
    category: "Tops",
    price: 95.0,
    stock: 18,
    image: "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?auto=format&fit=crop&w=600&q=80",
    description: "Effortless 90s-inspired slip dress cut from glossy silk-satin with delicate adjustable straps."
  },
  {
    id: 10,
    name: "Ankle Boots",
    category: "Shoes",
    price: 109.0,
    stock: 28,
    image: "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?auto=format&fit=crop&w=600&q=80",
    description: "Water-resistant Chelsea ankle boots with elastic side panels and durable lug soles."
  }
];

// Helper to get auth token
export function getToken() {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('styleshop_token');
}

// Helper to get stored user
export function getStoredUser() {
  if (typeof window === 'undefined') return null;
  const raw = localStorage.getItem('styleshop_user');
  try {
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

// Helper to save auth
export function saveAuth(user, token) {
  if (typeof window === 'undefined') return;
  if (token) localStorage.setItem('styleshop_token', token);
  if (user) localStorage.setItem('styleshop_user', JSON.stringify(user));
}

// Helper to clear auth
export function clearAuth() {
  if (typeof window === 'undefined') return;
  localStorage.removeItem('styleshop_token');
  localStorage.removeItem('styleshop_user');
}

// Core HTTP request wrapper
async function request(endpoint, options = {}) {
  const token = getToken();
  const headers = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const url = `${API_BASE}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  try {
    const res = await fetch(url, {
      ...options,
      headers,
    });

    const contentType = res.headers.get('content-type');
    const isJson = contentType && contentType.includes('application/json');
    const data = isJson ? await res.json() : await res.text();

    if (!res.ok) {
      const errorMsg = typeof data === 'object' && data.message ? data.message : (data || res.statusText);
      throw new Error(errorMsg);
    }

    return { success: true, data };
  } catch (err) {
    console.warn(`[API] Error on ${options.method || 'GET'} ${endpoint}:`, err.message);
    return { success: false, error: err.message };
  }
}

// ─────────────────────────────────────────────
// API Client
// ─────────────────────────────────────────────
export const api = {
  // Health
  async checkHealth() {
    try {
      const res = await fetch(`${API_BASE}/health`, { method: 'GET', signal: AbortSignal.timeout(3000) });
      return res.ok;
    } catch {
      return false;
    }
  },

  // Auth
  auth: {
    async login(username, password) {
      const res = await request('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
      });
      if (res.success && res.data.token) {
        saveAuth({
          userId: res.data.userId,
          username: res.data.username,
          email: res.data.email,
          roleId: res.data.roleId,
          role: res.data.roleId === 1 ? 'Admin' : (res.data.roleId === 2 ? 'Staff' : 'Customer')
        }, res.data.token);
      }
      return res;
    },

    async register(username, email, password, roleId = 3) {
      const res = await request('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ username, email, password, roleId }),
      });
      if (res.success && res.data.token) {
        saveAuth({
          userId: res.data.userId,
          username: res.data.username,
          email: res.data.email,
          roleId: res.data.roleId,
          role: res.data.roleId === 1 ? 'Admin' : (res.data.roleId === 2 ? 'Staff' : 'Customer')
        }, res.data.token);
      }
      return res;
    },

    async forgotPassword(username) {
      return await request('/api/auth/forgot-password', {
        method: 'POST',
        body: JSON.stringify({ username }),
      });
    },

    async resetPassword(token, newPassword) {
      return await request('/api/auth/reset-password', {
        method: 'POST',
        body: JSON.stringify({ token, newPassword }),
      });
    },

    logout() {
      clearAuth();
    }
  },

  // Products
  products: {
    async getAll() {
      const res = await request('/api/Product');
      if (res.success && Array.isArray(res.data) && res.data.length > 0) {
        return res.data;
      }
      return FALLBACK_PRODUCTS;
    },

    async getById(id) {
      const res = await request(`/api/Product/${id}`);
      if (res.success && res.data) {
        return res.data;
      }
      return FALLBACK_PRODUCTS.find(p => p.id === Number(id)) || FALLBACK_PRODUCTS[0];
    },

    async getByCategory(category) {
      const res = await request(`/api/Product/category/${encodeURIComponent(category)}`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return FALLBACK_PRODUCTS.filter(p => p.category.toLowerCase() === category.toLowerCase());
    },

    async search(query) {
      const res = await request(`/api/Product/search?query=${encodeURIComponent(query)}`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      const q = query.toLowerCase();
      return FALLBACK_PRODUCTS.filter(p =>
        p.name.toLowerCase().includes(q) ||
        p.category.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q))
      );
    },

    async create(product) {
      return await request('/api/Product', {
        method: 'POST',
        body: JSON.stringify(product),
      });
    },

    async update(id, product) {
      return await request(`/api/Product/${id}`, {
        method: 'PUT',
        body: JSON.stringify(product),
      });
    },

    async delete(id) {
      return await request(`/api/Product/${id}`, {
        method: 'DELETE',
      });
    }
  },

  // Cart
  cart: {
    async getByUser(userId) {
      const res = await request(`/api/Cart/user/${userId}`);
      if (res.success && res.data) {
        return res.data;
      }
      return { userId, items: [] };
    },

    async addItem(userId, productId, quantity = 1) {
      return await request('/api/Cart/items', {
        method: 'POST',
        body: JSON.stringify({ userId, productId, quantity }),
      });
    },

    async updateItem(cartItemId, quantity) {
      return await request(`/api/Cart/items/${cartItemId}`, {
        method: 'PUT',
        body: JSON.stringify({ quantity }),
      });
    },

    async removeItem(cartItemId) {
      return await request(`/api/Cart/items/${cartItemId}`, {
        method: 'DELETE',
      });
    }
  },

  // Orders
  orders: {
    async getAll() {
      const res = await request('/api/Order');
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { id: 1042, userId: 2, total: 211.99, orderDate: "2025-07-12T14:30:00Z", status: "Delivered" },
        { id: 1041, userId: 3, total: 89.00, orderDate: "2025-07-11T09:15:00Z", status: "Shipped" },
        { id: 1040, userId: 2, total: 145.50, orderDate: "2025-07-11T16:20:00Z", status: "Processing" },
        { id: 1039, userId: 3, total: 32.00, orderDate: "2025-07-10T11:45:00Z", status: "Delivered" },
        { id: 1038, userId: 2, total: 289.00, orderDate: "2025-07-09T18:00:00Z", status: "Cancelled" }
      ];
    },

    async getByUser(userId) {
      const res = await request(`/api/Order/user/${userId}`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { id: 1042, userId, total: 211.99, orderDate: "2025-07-12T14:30:00Z", status: "Delivered", itemsCount: 3 },
        { id: 1031, userId, total: 124.00, orderDate: "2025-06-28T10:00:00Z", status: "Shipped", itemsCount: 2 },
        { id: 1018, userId, total: 89.00, orderDate: "2025-06-10T15:20:00Z", status: "Delivered", itemsCount: 1 },
        { id: 1005, userId, total: 287.50, orderDate: "2025-05-22T13:40:00Z", status: "Delivered", itemsCount: 4 },
      ];
    },

    async getById(id) {
      return await request(`/api/Order/${id}`);
    },

    async getOrderItems(orderId) {
      const res = await request(`/api/Order/${orderId}/items`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { id: 1, orderId, productId: 1, quantity: 2, price: 29.0 },
        { id: 2, orderId, productId: 2, quantity: 1, price: 59.0 }
      ];
    },

    async create(orderData) {
      return await request('/api/Order', {
        method: 'POST',
        body: JSON.stringify(orderData),
      });
    },

    async update(id, updateData) {
      return await request(`/api/Order/${id}`, {
        method: 'PUT',
        body: JSON.stringify(updateData),
      });
    },

    async delete(id) {
      return await request(`/api/Order/${id}`, {
        method: 'DELETE',
      });
    }
  },

  // Coupons
  coupons: {
    async validate(code) {
      const res = await request(`/api/Coupon/validate/${encodeURIComponent(code)}`);
      if (res.success && res.data) {
        return res.data;
      }
      if (code.toUpperCase() === 'WELCOME20') {
        return { isValid: true, message: "Valid 20% discount coupon", coupon: { discount: 20, code: "WELCOME20" } };
      }
      return { isValid: false, message: "Invalid or expired coupon" };
    },

    async getAll() {
      const res = await request('/api/Coupon');
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { couponId: 1, code: "WELCOME20", discount: 20, expirationDate: "2030-12-31", status: "active" },
        { couponId: 2, code: "SUMMER15", discount: 15, expirationDate: "2026-09-30", status: "active" }
      ];
    }
  },

  // Addresses
  addresses: {
    async getByUser(userId) {
      const res = await request(`/api/Address/user/${userId}`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        {
          addressId: 1,
          userId,
          recipientName: "Jane Smith",
          phone: "+1 555 000 1234",
          street: "123 Main Street, Apt 4B",
          city: "New York",
          province: "NY",
          postalCode: "10001"
        }
      ];
    },

    async create(address) {
      return await request('/api/Address', {
        method: 'POST',
        body: JSON.stringify(address),
      });
    },

    async delete(id) {
      return await request(`/api/Address/${id}`, {
        method: 'DELETE',
      });
    }
  },

  // Reviews
  reviews: {
    async getByProduct(productId) {
      const res = await request(`/api/Review/product/${productId}`);
      if (res.success && Array.isArray(res.data) && res.data.length > 0) {
        return res.data;
      }
      return [
        { reviewId: 1, productId, userId: 2, rating: 5, comment: "Exceptional quality fabric and true to size fit. Will definitely buy again!", createdAt: "2025-07-02" },
        { reviewId: 2, productId, userId: 3, rating: 4, comment: "Very comfortable for everyday wear. Material feels premium.", createdAt: "2025-06-25" },
        { reviewId: 3, productId, userId: 4, rating: 5, comment: "Arrived quickly and looked even better than in photos.", createdAt: "2025-06-12" }
      ];
    },

    async create(review) {
      return await request('/api/Review', {
        method: 'POST',
        body: JSON.stringify(review),
      });
    }
  },

  // Notifications
  notifications: {
    async getByUser(userId) {
      const res = await request(`/api/Notification/user/${userId}`);
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { notificationId: 1, userId, message: "Your order #ORD-20250042 has been delivered.", isRead: false, createdAt: "2025-07-12T16:00:00Z" },
        { notificationId: 2, userId, message: "Use code WELCOME20 to get 20% off your next purchase.", isRead: true, createdAt: "2025-07-01T10:00:00Z" }
      ];
    },

    async markAsRead(id) {
      return await request(`/api/Notification/${id}/read`, { method: 'PUT' });
    }
  },

  // Admin Users
  users: {
    async getAll() {
      const res = await request('/api/Userman');
      if (res.success && Array.isArray(res.data)) {
        return res.data;
      }
      return [
        { userId: 1, userName: "system_admin", email: "admin@system.local", userRoleId: 1 },
        { userId: 2, userName: "admin", email: "admin@styleshop.com", userRoleId: 1 },
        { userId: 3, userName: "customer", email: "customer@styleshop.com", userRoleId: 3 }
      ];
    }
  }
};
