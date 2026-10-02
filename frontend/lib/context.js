'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';
import { api, getStoredUser, getToken, clearAuth } from './api';

const AppContext = createContext();

export function AppProvider({ children }) {
  // Screen routing state
  const [screen, setScreen] = useState('home');
  const [selectedProductId, setSelectedProductId] = useState(1);
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [searchQuery, setSearchQuery] = useState('');

  // Auth state
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isAuthLoading, setIsAuthLoading] = useState(true);

  // Cart state
  const [cartItems, setCartItems] = useState([
    {
      id: 1,
      productId: 1,
      name: "Classic White Tee",
      price: 29.0,
      size: "M",
      color: "#6b7280",
      quantity: 2,
      image: "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"
    },
    {
      id: 2,
      productId: 2,
      name: "Slim Chino Pants",
      price: 59.0,
      size: "L",
      color: "#374151",
      quantity: 1,
      image: "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?auto=format&fit=crop&w=600&q=80"
    },
    {
      id: 3,
      productId: 3,
      name: "Canvas Sneakers",
      price: 89.0,
      size: "42",
      color: "#111827",
      quantity: 1,
      image: "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?auto=format&fit=crop&w=600&q=80"
    }
  ]);
  const [appliedCoupon, setAppliedCoupon] = useState({ code: "WELCOME20", discount: 20 });

  // System & API health status
  const [apiOnline, setApiOnline] = useState(false);
  const [toast, setToast] = useState(null);

  // Show a temporary banner notification
  const showToast = (message, type = 'success') => {
    setToast({ message, type });
    setTimeout(() => {
      setToast(null);
    }, 3500);
  };

  // Check health on mount and every 30s
  useEffect(() => {
    let mounted = true;
    const checkStatus = async () => {
      const online = await api.checkHealth();
      if (mounted) setApiOnline(online);
    };
    checkStatus();
    const interval = setInterval(checkStatus, 20000);
    return () => {
      mounted = false;
      clearInterval(interval);
    };
  }, []);

  // Restore stored session and cart from localStorage
  useEffect(() => {
    const savedUser = getStoredUser();
    const savedToken = getToken();
    if (savedUser && savedToken) {
      setUser(savedUser);
      setToken(savedToken);
    }
    
    try {
      const savedCart = localStorage.getItem('styleshop_cart');
      if (savedCart) {
        const parsed = JSON.parse(savedCart);
        if (Array.isArray(parsed) && parsed.length > 0) {
          setCartItems(parsed);
        }
      }
    } catch (e) {
      console.warn("Failed to load saved cart", e);
    }
    
    setIsAuthLoading(false);
  }, []);

  // Sync cart to localStorage whenever it changes
  useEffect(() => {
    if (typeof window !== 'undefined') {
      localStorage.setItem('styleshop_cart', JSON.stringify(cartItems));
    }
  }, [cartItems]);

  // Auth actions
  const login = async (username, password) => {
    const res = await api.auth.login(username, password);
    if (res.success) {
      const savedUser = getStoredUser();
      const savedToken = getToken();
      setUser(savedUser);
      setToken(savedToken);
      showToast(`Welcome back, ${savedUser.username}!`);
      return { success: true };
    } else {
      showToast(res.error || "Login failed", "error");
      return { success: false, error: res.error };
    }
  };

  const register = async (username, email, password, roleId = 3) => {
    const res = await api.auth.register(username, email, password, roleId);
    if (res.success) {
      const savedUser = getStoredUser();
      const savedToken = getToken();
      setUser(savedUser);
      setToken(savedToken);
      showToast(`Account created! Welcome, ${username}.`);
      return { success: true };
    } else {
      showToast(res.error || "Registration failed", "error");
      return { success: false, error: res.error };
    }
  };

  const logout = () => {
    api.auth.logout();
    setUser(null);
    setToken(null);
    showToast("You have been signed out.");
    setScreen('home');
  };

  // Cart actions
  const addToCart = async (product, qty = 1, size = 'M', color = '#6b7280') => {
    const existingIdx = cartItems.findIndex(i => i.productId === product.id && i.size === size);
    
    if (existingIdx > -1) {
      const updated = [...cartItems];
      updated[existingIdx].quantity += qty;
      setCartItems(updated);
    } else {
      const newItem = {
        id: Date.now(),
        productId: product.id,
        name: product.name,
        price: product.price,
        size,
        color,
        quantity: qty,
        image: product.image || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"
      };
      setCartItems(prev => [newItem, ...prev]);
    }

    // Also persist to API if user is logged in
    if (user?.userId) {
      api.cart.addItem(user.userId, product.id, qty).catch(() => {});
    }

    showToast(`Added "${product.name}" to cart`);
  };

  const updateCartQuantity = (id, newQty) => {
    if (newQty <= 0) {
      removeFromCart(id);
      return;
    }
    setCartItems(prev => prev.map(item => item.id === id ? { ...item, quantity: newQty } : item));
  };

  const removeFromCart = (id) => {
    setCartItems(prev => prev.filter(item => item.id !== id));
    showToast("Item removed from cart", "info");
  };

  const clearCart = () => {
    setCartItems([]);
  };

  const applyCouponCode = async (code) => {
    if (!code || !code.trim()) return { success: false, message: "Please enter a coupon code." };
    const res = await api.coupons.validate(code.trim());
    if (res.isValid) {
      const disc = res.coupon?.discount || 20;
      setAppliedCoupon({ code: code.trim().toUpperCase(), discount: disc });
      showToast(`Coupon applied! ${disc}% discount added.`);
      return { success: true, message: `Applied ${disc}% discount` };
    } else {
      showToast(res.message || "Invalid coupon code", "error");
      return { success: false, message: res.message || "Invalid coupon" };
    }
  };

  const removeCoupon = () => {
    setAppliedCoupon(null);
    showToast("Coupon removed", "info");
  };

  // Cart calculations
  const subtotal = cartItems.reduce((acc, item) => acc + (item.price * item.quantity), 0);
  const discountAmount = appliedCoupon ? (subtotal * (appliedCoupon.discount / 100)) : 0;
  const shippingFee = subtotal > 150 || subtotal === 0 ? 0 : 5.99;
  const total = Math.max(0, subtotal - discountAmount + shippingFee);
  const totalItemCount = cartItems.reduce((acc, item) => acc + item.quantity, 0);

  // Screen navigation helper
  const navigateTo = (targetScreen, params = {}) => {
    if (params.productId) setSelectedProductId(params.productId);
    if (params.category) setSelectedCategory(params.category);
    if (params.search !== undefined) setSearchQuery(params.search);
    setScreen(targetScreen);
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const value = {
    screen,
    navigateTo,
    selectedProductId,
    setSelectedProductId,
    selectedCategory,
    setSelectedCategory,
    searchQuery,
    setSearchQuery,
    // Auth
    user,
    token,
    isAuthLoading,
    isLoggedIn: !!user,
    isAdmin: user?.roleId === 1 || user?.role === 'Admin',
    login,
    register,
    logout,
    // Cart
    cartItems,
    addToCart,
    updateCartQuantity,
    removeFromCart,
    clearCart,
    appliedCoupon,
    applyCouponCode,
    removeCoupon,
    subtotal,
    discountAmount,
    shippingFee,
    total,
    totalItemCount,
    // Status
    apiOnline,
    toast,
    showToast,
  };

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error('useApp must be used within an AppProvider');
  }
  return context;
}
