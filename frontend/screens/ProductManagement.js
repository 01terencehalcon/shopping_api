'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api, FALLBACK_PRODUCTS, resolveImageUrl } from '../lib/api';
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
  LogOut,
  Upload,
  Image as ImageIcon,
  Shield,
  Star,
  Check
} from 'lucide-react';

export default function ProductManagement() {
  const { navigateTo, logout, showToast, user, canManageProducts, isAuthLoading } = useApp();

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
    image: '',
    description: '',
  });
  const [saving, setSaving] = useState(false);
  const [productImages, setProductImages] = useState([]);
  const [pendingFiles, setPendingFiles] = useState([]); // [{ file, previewUrl }]
  const [uploadingImages, setUploadingImages] = useState(false);

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

  const loadProductImages = async (productId) => {
    try {
      const imgs = await api.productImages.getByProductId(productId);
      setProductImages(Array.isArray(imgs) ? imgs : []);
    } catch (err) {
      console.warn("Product images load error:", err);
      setProductImages([]);
    }
  };

  // Redirect non-staff/admin users to /login
  useEffect(() => {
    if (!isAuthLoading) {
      const isStaffOrAdmin = user && (user.roleId === 1 || user.roleId === 2 || user.role === 'Admin' || user.role === 'Staff');
      if (!isStaffOrAdmin) {
        if (typeof window !== 'undefined') {
          window.location.href = '/login';
        }
      }
    }
  }, [user, isAuthLoading]);

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
      image: '',
      description: 'Handcrafted garment tailored with premium fabrics.',
    });
    setProductImages([]);
    setPendingFiles([]);
    setModalMode('add');
  };

  const handleOpenEdit = async (p) => {
    setCurrentProduct({ ...p });
    setPendingFiles([]);
    setModalMode('edit');
    await loadProductImages(p.id);
  };

  const handleFileSelect = async (e) => {
    const files = Array.from(e.target.files || []);
    if (files.length === 0) return;
    e.target.value = '';

    const validFiles = [];
    for (const file of files) {
      if (file.size > 2 * 1024 * 1024) {
        showToast(`File "${file.name}" exceeds the 2 MB limit`, "error");
        continue;
      }
      const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      if (!['.jpg', '.jpeg', '.png', '.webp'].includes(ext)) {
        showToast(`"${file.name}" is not a valid jpg, png, or webp image`, "error");
        continue;
      }
      validFiles.push(file);
    }

    if (validFiles.length === 0) return;

    if (modalMode === 'edit') {
      if (productImages.length + validFiles.length > 6) {
        showToast(`A product can have at most 6 images (currently ${productImages.length})`, "error");
        return;
      }

      setUploadingImages(true);
      try {
        const res = await api.productImages.upload(currentProduct.id, validFiles);
        if (res.success) {
          showToast(`Uploaded ${validFiles.length} image(s)!`);
          await loadProductImages(currentProduct.id);
          await fetchProducts();
        } else {
          showToast(res.error || "Failed to upload images", "error");
        }
      } catch (err) {
        showToast(err.message || "Upload failed", "error");
      } finally {
        setUploadingImages(false);
      }
    } else {
      if (pendingFiles.length + validFiles.length > 6) {
        showToast(`A product can have at most 6 images (currently ${pendingFiles.length})`, "error");
        return;
      }
      const newPending = validFiles.map(file => ({
        file,
        previewUrl: URL.createObjectURL(file)
      }));
      setPendingFiles(prev => [...prev, ...newPending]);
    }
  };

  const handleRemovePendingFile = (idx) => {
    setPendingFiles(prev => {
      const removed = prev[idx];
      if (removed?.previewUrl) URL.revokeObjectURL(removed.previewUrl);
      return prev.filter((_, i) => i !== idx);
    });
  };

  const handleSetMainImage = async (imageId, imageUrl) => {
    if (!currentProduct.id) return;
    try {
      const res = await api.productImages.setMain(currentProduct.id, imageId);
      if (res.success) {
        setCurrentProduct(prev => ({ ...prev, image: imageUrl }));
        showToast("Set as main product image!");
        await fetchProducts();
      } else {
        showToast(res.error || "Failed to set main image", "error");
      }
    } catch (err) {
      showToast(err.message || "Failed to set main image", "error");
    }
  };

  const handleDeleteImage = async (imageId) => {
    try {
      const res = await api.productImages.delete(imageId);
      if (res.success) {
        showToast("Image removed");
        await loadProductImages(currentProduct.id);
        await fetchProducts();
      } else {
        showToast(res.error || "Failed to delete image", "error");
      }
    } catch (err) {
      showToast(err.message || "Failed to delete image", "error");
    }
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
          image: currentProduct.image || '',
          description: currentProduct.description,
        });

        if (res.success) {
          const newId = res.data?.id;
          if (newId && pendingFiles.length > 0) {
            const filesToUpload = pendingFiles.map(p => p.file);
            await api.productImages.upload(newId, filesToUpload);
          }
          showToast("Product created successfully!");
        } else {
          showToast(res.error || "Failed to create product", "error");
        }
      } else {
        await api.products.update(currentProduct.id, {
          name: currentProduct.name,
          category: currentProduct.category,
          price: Number(currentProduct.price),
          stock: Number(currentProduct.stock),
          image: currentProduct.image,
          description: currentProduct.description,
        });
        showToast("Product updated successfully!");
      }
      setModalMode(null);
      await fetchProducts();
    } catch (err) {
      showToast(err.message || "Failed to save product", "error");
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

  if (!isAuthLoading && !canManageProducts) {
    return (
      <div className="min-h-[calc(100vh-40px)] bg-gray-50 flex items-center justify-center p-6">
        <div className="max-w-md w-full bg-white rounded-2xl border border-gray-200 p-8 shadow-sm text-center animate-in fade-in zoom-in-95">
          <div className="w-14 h-14 mx-auto rounded-2xl bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-600 mb-5">
            <Shield size={28} />
          </div>
          <h2 className="text-xl font-bold text-gray-900 tracking-tight">Staff & Admin Access Only</h2>
          <p className="text-sm text-gray-500 mt-2 leading-relaxed">
            Product Catalog Management is restricted to verified <strong>Staff</strong> and <strong>Administrators</strong>. Customers cannot post, edit, or manage products.
          </p>

          <div className="my-6 p-3.5 bg-gray-50 rounded-xl border border-gray-200 text-xs text-gray-600 flex items-center justify-between">
            <span className="text-gray-400">Current Account:</span>
            <span className="font-semibold text-gray-800">
              {user ? (
                <span>
                  {user.roleId === 1 ? '👑 Administrator' : user.roleId === 2 ? '🛡️ Staff' : '👤 Customer'} ({user.username})
                </span>
              ) : (
                'Not Signed In'
              )}
            </span>
          </div>

          <div className="flex flex-col sm:flex-row gap-2.5">
            <button
              onClick={() => navigateTo('home')}
              className="flex-1 px-4 py-2.5 bg-gray-900 hover:bg-gray-800 text-white text-xs font-semibold rounded-xl transition cursor-pointer"
            >
              Back to Storefront
            </button>
            <button
              onClick={() => navigateTo('login')}
              className="px-4 py-2.5 bg-gray-100 hover:bg-gray-200 text-gray-800 text-xs font-semibold rounded-xl border border-gray-200 transition cursor-pointer"
            >
              Sign In as Staff / Admin
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-[calc(100vh-40px)] bg-gray-100">
      
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 min-h-full flex-shrink-0 flex flex-col text-neutral-300">
        <div className="px-5 py-5 border-b border-gray-800">
          <p className="text-[10px] font-black text-gray-400 uppercase tracking-widest">STYLESHOP</p>
          <p className="text-sm font-bold text-white mt-0.5">Staff & Admin Portal</p>
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
          {user && (
            <div className="px-3 py-2 text-[11px] text-gray-400 mb-1">
              <span className="text-gray-500 block text-[10px] uppercase font-bold tracking-wider">Signed in as:</span>
              <span className="text-neutral-200 font-semibold">{user.roleId === 1 ? '👑 Admin' : '🛡️ Staff'} ({user.username})</span>
            </div>
          )}
          <button
            onClick={logout}
            className="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-xs font-semibold text-gray-400 hover:text-red-400 hover:bg-gray-800 transition cursor-pointer"
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
                            src={resolveImageUrl(p.image) || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
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
                      <td className="px-4 py-3.5 font-bold text-gray-900">₱{Number(p.price).toFixed(2)}</td>
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
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4 overflow-y-auto">
          <form onSubmit={handleSaveProduct} className="bg-white rounded-2xl max-w-xl w-full p-6 space-y-4 shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-center pb-2 border-b border-gray-100">
              <h3 className="text-sm font-bold text-gray-900">
                {modalMode === 'add' ? 'Create New Garment' : `Edit Product #${currentProduct.id}`}
              </h3>
              <button type="button" onClick={() => setModalMode(null)} className="text-gray-400 hover:text-gray-600">
                <X size={16} />
              </button>
            </div>

            <div className="space-y-3.5 text-xs">
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
                  <label className="text-gray-700 font-semibold block mb-1">Price (₱ PHP)</label>
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

              {/* Multi-Image Gallery Management */}
              <div className="border border-gray-200 rounded-xl p-3.5 bg-gray-50/70 space-y-3">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-gray-800 font-bold block">
                      Product Photos ({modalMode === 'edit' ? productImages.length : pendingFiles.length} of 6)
                    </label>
                    <p className="text-[11px] text-gray-500">
                      Upload up to 6 pictures (JPG, PNG, or WebP &le; 2 MB).
                    </p>
                  </div>
                  {((modalMode === 'edit' ? productImages.length : pendingFiles.length) < 6) && (
                    <label className={`inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-gray-300 hover:border-gray-900 text-gray-800 font-semibold rounded-lg shadow-2xs transition cursor-pointer text-xs ${uploadingImages ? 'opacity-50 cursor-not-allowed' : ''}`}>
                      <Upload size={13} />
                      <span>{uploadingImages ? 'Uploading...' : 'Add Images'}</span>
                      <input
                        type="file"
                        multiple
                        accept="image/jpeg,image/png,image/webp"
                        disabled={uploadingImages}
                        onChange={handleFileSelect}
                        className="hidden"
                      />
                    </label>
                  )}
                </div>

                {/* Existing Images Grid (Edit Mode) */}
                {modalMode === 'edit' && productImages.length > 0 && (
                  <div className="grid grid-cols-3 sm:grid-cols-4 gap-2.5 pt-1">
                    {productImages.map((img) => {
                      const isMain = currentProduct.image === img.imageUrl;
                      return (
                        <div
                          key={img.imageId}
                          className={`relative group bg-white rounded-lg border overflow-hidden p-1 flex flex-col items-center shadow-2xs transition ${
                            isMain ? 'border-gray-900 ring-2 ring-gray-900/20' : 'border-gray-200 hover:border-gray-400'
                          }`}
                        >
                          <img
                            src={resolveImageUrl(img.imageUrl)}
                            alt="Product photo"
                            className="w-full h-24 object-cover rounded"
                          />
                          <div className="w-full mt-1.5 flex items-center justify-between text-[10px]">
                            {isMain ? (
                              <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-gray-900 text-white font-bold rounded">
                                <Check size={10} /> Main
                              </span>
                            ) : (
                              <button
                                type="button"
                                onClick={() => handleSetMainImage(img.imageId, img.imageUrl)}
                                className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold rounded transition"
                                title="Set as Main Thumbnail"
                              >
                                <Star size={10} /> Make Main
                              </button>
                            )}
                            <button
                              type="button"
                              onClick={() => handleDeleteImage(img.imageId)}
                              className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition"
                              title="Delete photo"
                            >
                              <Trash2 size={12} />
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}

                {/* Pending Uploads Grid (Add Mode) */}
                {modalMode === 'add' && pendingFiles.length > 0 && (
                  <div className="grid grid-cols-3 sm:grid-cols-4 gap-2.5 pt-1">
                    {pendingFiles.map((item, idx) => (
                      <div
                        key={idx}
                        className="relative group bg-white rounded-lg border border-gray-200 overflow-hidden p-1 flex flex-col items-center shadow-2xs"
                      >
                        <img
                          src={item.previewUrl}
                          alt={`Upload preview ${idx + 1}`}
                          className="w-full h-24 object-cover rounded"
                        />
                        <div className="w-full mt-1.5 flex items-center justify-between text-[10px]">
                          <span className="text-gray-500 font-medium">
                            {idx === 0 ? 'Main Photo' : `#${idx + 1}`}
                          </span>
                          <button
                            type="button"
                            onClick={() => handleRemovePendingFile(idx)}
                            className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition"
                            title="Remove"
                          >
                            <X size={12} />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}

                {/* Empty State */}
                {((modalMode === 'edit' && productImages.length === 0) ||
                  (modalMode === 'add' && pendingFiles.length === 0)) && (
                  <div className="text-center py-4 bg-white rounded-lg border border-dashed border-gray-300">
                    <ImageIcon size={24} className="mx-auto text-gray-400 mb-1" />
                    <p className="text-xs text-gray-600 font-medium">No pictures uploaded yet</p>
                    <p className="text-[10px] text-gray-400">Click &ldquo;Add Images&rdquo; above to select files from your computer</p>
                  </div>
                )}

                {/* Optional Fallback URL */}
                <div className="pt-1">
                  <label className="text-[11px] text-gray-500 font-medium block mb-1">
                    Or external thumbnail URL (optional):
                  </label>
                  <input
                    type="url"
                    placeholder="https://..."
                    value={currentProduct.image}
                    onChange={(e) => setCurrentProduct({ ...currentProduct, image: e.target.value })}
                    className="w-full px-3 py-1.5 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
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
