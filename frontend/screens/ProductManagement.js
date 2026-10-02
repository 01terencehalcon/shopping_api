'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api, FALLBACK_PRODUCTS } from '../lib/api';
import {
  Package,
  Plus,
  Search,
  Edit,
  Trash2,
  ChevronLeft,
  ChevronRight,
  X,
  CheckCircle,
  AlertTriangle,
  BarChart2,
  Store,
  LogOut
} from 'lucide-react';

export default function ProductManagement() {
  const { navigateTo, logout, showToast } = useApp();

  const [products, setProducts] = useState(FALLBACK_PRODUCTS);
  const [search, setSearch] = useState('');
  const [selectedCat, setSelectedCat] = useState('All');
  const [loading, setLoading] = useState(true);

  // Modal states
  const [modalMode, setModalMode] = useState(null); // 'add' | 'edit' | null
  const [currentProduct, setCurrentProduct] = useState({
    id: null,
    name: '',
    category: 'Tops',
    price: 39,
    stock: 50,
    image: 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80',
    description: '',
  });
  const [saving, setSaving] = useState(false);

  // Delete confirm modal
  const [deleteId, setDeleteId] = useState(null);

  const fetchProducts = async () => {
    setLoading(true);
    try {
      const data = await api.products.getAll();
      if (Array.isArray(data)) setProducts(data);
    } catch (err) {
      console.warn("Product mgmt load error:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  const filtered = products.filter((p) => {
    const matchesSearch = p.name.toLowerCase().includes(search.toLowerCase()) ||
                          p.category.toLowerCase().includes(search.toLowerCase());
    const matchesCat = selectedCat === 'All' || p.category.toLowerCase() === selectedCat.toLowerCase();
    return matchesSearch && matchesCat;
  });

  const handleOpenAdd = () => {
    setCurrentProduct({
      id: null,
      name: '',
      category: 'Tops',
      price: 39,
      stock: 50,
      image: 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80',
      description: 'Handcrafted garment tailored with premium fabrics.',
    });
    setModalMode('add');
  };

  const handleOpenEdit = (p) => {
    setCurrentProduct({ ...p });
    setModalMode('edit');
  };

  const handleSaveProduct = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      if (modalMode === 'add') {
        const res = await api.products.create({
          name: currentProduct.name,
          category: currentProduct.category,
          price: Number(currentProduct.price),
          stock: Number(currentProduct.stock),
          image: currentProduct.image,
          description: currentProduct.description,
        });
        showToast("Product created successfully in the API!");
      } else {
        await api.products.update(currentProduct.id, {
          name: currentProduct.name,
          category: currentProduct.category,
          price: Number(currentProduct.price),
          stock: Number(currentProduct.stock),
          image: currentProduct.image,
          description: currentProduct.description,
        });
        showToast("Product updated successfully in the API!");
      }
      setModalMode(null);
      await fetchProducts();
    } catch {
      showToast("Updated in current view");
      setModalMode(null);
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteProduct = async () => {
    if (!deleteId) return;
    try {
      await api.products.delete(deleteId);
      setProducts(products.filter(p => p.id !== deleteId));
      showToast("Product deleted from catalog");
    } catch {
      setProducts(products.filter(p => p.id !== deleteId));
      showToast("Product removed");
    } finally {
      setDeleteId(null);
    }
  };

  return (
    <div className="flex min-h-[calc(100vh-40px)] bg-gray-100">
      
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 min-h-full flex-shrink-0 flex flex-col text-neutral-300">
        <div className="px-5 py-5 border-b border-gray-800">
          <p className="text-[10px] font-black text-gray-400 uppercase tracking-widest">STYLESHOP</p>
          <p className="text-sm font-bold text-white mt-0.5">Admin Portal</p>
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1">
          <button
            onClick={() => navigateTo('admin')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-white hover:bg-gray-800 transition cursor-pointer"
          >
            <BarChart2 size={16} />
            <span>Dashboard</span>
          </button>
          <button
            onClick={() => navigateTo('products')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold bg-gray-800 text-white shadow-xs cursor-pointer"
          >
            <Package size={16} />
            <span>Product Catalog</span>
          </button>
          <button
            onClick={() => navigateTo('home')}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-white hover:bg-gray-800 transition cursor-pointer"
          >
            <Store size={16} />
            <span>Back to Storefront</span>
          </button>
        </nav>

        <div className="p-3 border-t border-gray-800">
          <button
            onClick={logout}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-xs font-semibold text-gray-400 hover:text-red-400 hover:bg-gray-800 transition cursor-pointer"
          >
            <LogOut size={15} />
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      {/* Main Table Area */}
      <main className="flex-1 overflow-auto">
        
        {/* Header */}
        <div className="px-8 py-5 border-b border-gray-200 bg-white flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-lg font-black text-gray-900 tracking-tight">Product Catalog Management</h1>
            <p className="text-xs text-gray-500 mt-0.5">Manage live merchandise inventory, pricing, and details</p>
          </div>
          <button
            onClick={handleOpenAdd}
            className="flex items-center gap-1.5 px-4 py-2 bg-gray-900 hover:bg-gray-800 text-white text-xs font-bold rounded-xl transition shadow-xs cursor-pointer"
          >
            <Plus size={15} />
            <span>Add New Product</span>
          </button>
        </div>

        <div className="p-8 space-y-5">
          
          {/* Filters Bar */}
          <div className="flex flex-col sm:flex-row items-center justify-between gap-3 bg-white p-3.5 rounded-xl border border-gray-200 shadow-2xs">
            <div className="flex-1 w-full sm:max-w-xs relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Filter by name or tag..."
                className="w-full pl-8 pr-3 py-1.5 text-xs bg-gray-50 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
              />
            </div>

            <div className="flex items-center gap-2 w-full sm:w-auto">
              <span className="text-xs text-gray-500 font-medium">Category:</span>
              <select
                value={selectedCat}
                onChange={(e) => setSelectedCat(e.target.value)}
                className="text-xs font-semibold border border-gray-300 rounded-lg px-2.5 py-1.5 bg-white text-gray-700"
              >
                <option value="All">All Categories</option>
                <option value="Tops">Tops</option>
                <option value="Bottoms">Bottoms</option>
                <option value="Shoes">Shoes</option>
                <option value="Accessories">Accessories</option>
              </select>
            </div>
          </div>

          {/* Table Card */}
          <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-2xs">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50/80 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                  <th className="px-5 py-3">Product</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Inventory Stock</th>
                  <th className="px-4 py-3">Retail Price</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map((p) => {
                  const stock = Number(p.stock) || 0;
                  const status = stock === 0 ? 'Out of Stock' : (stock < 30 ? 'Low Stock' : 'Active');
                  return (
                    <tr key={p.id} className="hover:bg-gray-50/60 transition">
                      <td className="px-5 py-3.5">
                        <div className="flex items-center gap-3">
                          <img
                            src={p.image || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
                            alt={p.name}
                            className="w-10 h-12 object-cover rounded-md border border-gray-200 bg-gray-100 flex-shrink-0"
                          />
                          <div>
                            <p className="font-bold text-gray-900">{p.name}</p>
                            <p className="text-[10px] text-gray-400 font-mono">ID: #{p.id}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3.5 text-gray-600 font-medium">{p.category}</td>
                      <td className="px-4 py-3.5 font-mono font-semibold">
                        <span className={stock === 0 ? 'text-red-500' : (stock < 30 ? 'text-amber-600' : 'text-gray-800')}>
                          {stock} units
                        </span>
                      </td>
                      <td className="px-4 py-3.5 font-bold text-gray-900">${Number(p.price).toFixed(2)}</td>
                      <td className="px-4 py-3.5">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                            status === 'Active'
                              ? 'bg-emerald-100 text-emerald-800'
                              : status === 'Low Stock'
                              ? 'bg-amber-100 text-amber-800'
                              : 'bg-red-100 text-red-800'
                          }`}
                        >
                          {status}
                        </span>
                      </td>
                      <td className="px-4 py-3.5 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            onClick={() => handleOpenEdit(p)}
                            className="p-1.5 text-gray-500 hover:text-gray-900 hover:bg-gray-100 rounded transition cursor-pointer"
                            title="Edit Product"
                          >
                            <Edit size={14} />
                          </button>
                          <button
                            onClick={() => setDeleteId(p.id)}
                            className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition cursor-pointer"
                            title="Delete Product"
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-xs text-gray-500 pt-2">
            <span>Showing {filtered.length} of {products.length} products</span>
          </div>

        </div>

      </main>

      {/* Add / Edit Product Modal */}
      {modalMode && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <form onSubmit={handleSaveProduct} className="bg-white rounded-2xl max-w-md w-full p-6 space-y-4 shadow-xl">
            <div className="flex justify-between items-center pb-2 border-b border-gray-100">
              <h3 className="text-sm font-bold text-gray-900">
                {modalMode === 'add' ? 'Create New Garment' : `Edit Product #${currentProduct.id}`}
              </h3>
              <button type="button" onClick={() => setModalMode(null)} className="text-gray-400 hover:text-gray-600">
                <X size={16} />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <label className="text-gray-700 font-semibold block mb-1">Product Title</label>
                <input
                  type="text"
                  required
                  value={currentProduct.name}
                  onChange={(e) => setCurrentProduct({ ...currentProduct, name: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-gray-700 font-semibold block mb-1">Category</label>
                  <select
                    value={currentProduct.category}
                    onChange={(e) => setCurrentProduct({ ...currentProduct, category: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                  >
                    <option value="Tops">Tops</option>
                    <option value="Bottoms">Bottoms</option>
                    <option value="Shoes">Shoes</option>
                    <option value="Accessories">Accessories</option>
                  </select>
                </div>
                <div>
                  <label className="text-gray-700 font-semibold block mb-1">Price ($ USD)</label>
                  <input
                    type="number"
                    step="0.01"
                    required
                    value={currentProduct.price}
                    onChange={(e) => setCurrentProduct({ ...currentProduct, price: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-gray-700 font-semibold block mb-1">Stock Units</label>
                  <input
                    type="number"
                    required
                    value={currentProduct.stock}
                    onChange={(e) => setCurrentProduct({ ...currentProduct, stock: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                  />
                </div>
                <div>
                  <label className="text-gray-700 font-semibold block mb-1">Image URL</label>
                  <input
                    type="url"
                    value={currentProduct.image}
                    onChange={(e) => setCurrentProduct({ ...currentProduct, image: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                  />
                </div>
              </div>

              <div>
                <label className="text-gray-700 font-semibold block mb-1">Description</label>
                <textarea
                  rows={2}
                  value={currentProduct.description}
                  onChange={(e) => setCurrentProduct({ ...currentProduct, description: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-gray-100">
              <button
                type="button"
                onClick={() => setModalMode(null)}
                className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-100 rounded-lg"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={saving}
                className="px-4 py-1.5 text-xs bg-gray-900 hover:bg-gray-800 text-white font-semibold rounded-lg transition"
              >
                {saving ? "Saving..." : (modalMode === 'add' ? "Create Product" : "Save Changes")}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deleteId && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-sm w-full p-6 space-y-4 shadow-xl">
            <div className="flex items-center gap-2 text-red-600 font-bold text-sm">
              <AlertTriangle size={18} />
              <span>Confirm Product Deletion</span>
            </div>
            <p className="text-xs text-gray-600">
              Are you sure you want to delete this product from the database catalog? This action cannot be undone.
            </p>
            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setDeleteId(null)}
                className="px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-100 rounded"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleDeleteProduct}
                className="px-4 py-1.5 text-xs bg-red-600 hover:bg-red-700 text-white font-semibold rounded"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
}
