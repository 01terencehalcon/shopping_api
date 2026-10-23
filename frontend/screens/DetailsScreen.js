'use client';

import React, { useState, useEffect } from 'react';
import { useApp } from '../lib/context';
import { api, FALLBACK_PRODUCTS, resolveImageUrl } from '../lib/api';
import StoreHeader from '../components/StoreHeader';
import Footer from '../components/Footer';
import {
  Minus,
  Plus,
  ShoppingCart,
  Star,
  Check,
  RotateCcw,
  Truck,
  Ruler,
  Share2,
  Heart,
  MessageSquarePlus
} from 'lucide-react';

const SIZES = ['XS', 'S', 'M', 'L', 'XL'];
const COLORS = [
  { name: 'Light Gray', hex: '#d1d5db' },
  { name: 'Slate', hex: '#6b7280' },
  { name: 'Charcoal', hex: '#374151' },
  { name: 'Midnight', hex: '#111827' },
];

export default function DetailsScreen() {
  const { selectedProductId, navigateTo, addToCart, user, isLoggedIn, setRedirectAfterLogin, showToast } = useApp();

  const [product, setProduct] = useState(FALLBACK_PRODUCTS[0]);
  const [productImages, setProductImages] = useState([]);
  const [related, setRelated] = useState(FALLBACK_PRODUCTS.slice(1, 5));
  const [reviews, setReviews] = useState([]);
  const [loading, setLoading] = useState(true);

  // User selections
  const [selectedSize, setSelectedSize] = useState('M');
  const [selectedColor, setSelectedColor] = useState(COLORS[1].hex);
  const [quantity, setQuantity] = useState(1);
  const [activeThumb, setActiveThumb] = useState(0);

  // New review form
  const [showReviewForm, setShowReviewForm] = useState(false);
  const [newRating, setNewRating] = useState(5);
  const [newComment, setNewComment] = useState('');
  const [submittingReview, setSubmittingReview] = useState(false);

  useEffect(() => {
    let mounted = true;
    async function loadProductData() {
      setLoading(true);
      try {
        const id = selectedProductId || 1;
        const p = await api.products.getById(id);
        if (mounted && p) {
          setProduct(p);
        }

        // Fetch product images from database
        const imgs = await api.productImages.getByProductId(id);
        if (mounted) {
          setProductImages(Array.isArray(imgs) ? imgs : []);
          setActiveThumb(0);
        }

        // Fetch reviews
        const revs = await api.reviews.getByProduct(id);
        if (mounted && Array.isArray(revs)) {
          setReviews(revs);
        }

        // Fetch all products for related items
        const all = await api.products.getAll();
        if (mounted && Array.isArray(all)) {
          setRelated(all.filter(item => item.id !== id).slice(0, 4));
        }
      } catch (err) {
        console.warn("Details load error:", err);
      } finally {
        if (mounted) setLoading(false);
      }
    }
    loadProductData();
    return () => { mounted = false; };
  }, [selectedProductId]);

  const handleAddToCart = () => {
    addToCart(product, quantity, selectedSize, selectedColor);
  };

  const handleBuyNow = () => {
    addToCart(product, quantity, selectedSize, selectedColor);
    navigateTo('checkout');
  };

  const handleAddReview = async (e) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    setSubmittingReview(true);
    try {
      const res = await api.reviews.create({
        productId: product.id,
        rating: newRating,
        comment: newComment.trim(),
      });

      if (res?.success) {
        showToast("Thank you! Your review has been submitted.");
        setNewComment('');
        setShowReviewForm(false);

        // Fetch fresh reviews from backend to ensure state consistency and avoid key collisions
        const freshRevs = await api.reviews.getByProduct(product.id);
        if (Array.isArray(freshRevs)) {
          setReviews(freshRevs);
        } else {
          const newId = res?.data?.reviewId || Date.now();
          const createdReview = {
            reviewId: newId,
            productId: product.id,
            userId: user?.userId || 1,
            rating: newRating,
            comment: newComment.trim(),
            reviewDate: new Date().toISOString().split('T')[0],
          };
          setReviews(prev => [createdReview, ...prev.filter(r => r.reviewId !== newId)]);
        }
      } else {
        showToast(res?.error || "Failed to submit review", "error");
      }
    } catch (err) {
      showToast("Failed to submit review", "error");
    } finally {
      setSubmittingReview(false);
    }
  };

  // Multiple pictures for this product (from database / uploads, fallback to product.image)
  const galleryImages = (productImages && productImages.length > 0)
    ? productImages.map(img => resolveImageUrl(img.imageUrl))
    : [resolveImageUrl(product?.image) || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=800&q=80"];

  const currentDisplayImage = galleryImages[activeThumb] || galleryImages[0];

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <StoreHeader />

      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8 flex-1 w-full">
        
        {/* Breadcrumb Navigation */}
        <nav className="text-xs text-gray-500 mb-6 flex items-center gap-1.5">
          <button onClick={() => navigateTo('home')} className="hover:text-gray-900 transition">
            Home
          </button>
          <span>/</span>
          <button
            onClick={() => navigateTo('listing', { category: product.category })}
            className="hover:text-gray-900 transition"
          >
            {product.category || 'Collection'}
          </button>
          <span>/</span>
          <span className="text-gray-900 font-semibold truncate max-w-[200px]">
            {product.name}
          </span>
        </nav>

        {/* Main Product Showcase Grid */}
        <div className="bg-white border border-gray-200 rounded-2xl p-6 sm:p-8 shadow-xs grid grid-cols-1 md:grid-cols-2 gap-8 lg:gap-12">
          
          {/* Left Column: Image Gallery */}
          <div className="flex flex-col-reverse sm:flex-row gap-4">
            
            {/* Thumbnails */}
            {galleryImages.length > 1 && (
              <div className="flex sm:flex-col gap-2.5 overflow-x-auto sm:overflow-visible">
                {galleryImages.map((imgUrl, idx) => (
                  <button
                    key={idx}
                    onClick={() => setActiveThumb(idx)}
                    className={`w-16 h-16 sm:w-18 sm:h-18 rounded-lg overflow-hidden border-2 transition-all flex-shrink-0 cursor-pointer ${
                      activeThumb === idx ? 'border-gray-900 ring-2 ring-gray-300' : 'border-gray-200 opacity-70 hover:opacity-100'
                    }`}
                  >
                    <img src={imgUrl} alt={`Thumbnail ${idx + 1}`} className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
            )}

            {/* Main Stage Image */}
            <div className="flex-1 bg-gray-100 rounded-xl overflow-hidden border border-gray-200 aspect-3/4 max-h-[500px] relative group">
              <img
                src={currentDisplayImage}
                alt={product.name}
                className="w-full h-full object-cover group-hover:scale-103 transition duration-500"
              />
              <span className="absolute top-3 left-3 px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider bg-white/95 backdrop-blur-xs text-gray-900 rounded-md border border-gray-200 shadow-2xs">
                {product.category}
              </span>
            </div>

          </div>

          {/* Right Column: Product Details & Controls */}
          <div className="space-y-6 flex flex-col justify-between">
            <div className="space-y-5">
              
              <div>
                <span className="text-xs font-semibold text-gray-400 uppercase tracking-widest">
                  StyleShop Studio Collection
                </span>
                <h1 className="text-2xl sm:text-3xl font-black text-gray-900 mt-1 tracking-tight">
                  {product.name}
                </h1>
                
                <div className="flex items-center gap-3 mt-2">
                  <p className="text-2xl font-black text-gray-900">
                    ₱{Number(product.price).toFixed(2)}
                  </p>
                  <div className="flex items-center gap-1 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded text-xs font-semibold text-amber-900">
                    <Star size={13} className="fill-amber-400 text-amber-500" />
                    <span>4.8</span>
                    <span className="text-amber-700/70">({reviews.length} {reviews.length === 1 ? 'review' : 'reviews'})</span>
                  </div>
                </div>
              </div>

              {/* Description */}
              <div className="border-t border-gray-100 pt-4">
                <p className="text-sm text-gray-600 leading-relaxed">
                  {product.description || "A wardrobe essential crafted in premium fabric with modern drape and reinforced stitching for supreme durability and effortless style."}
                </p>
              </div>

              {/* Size Selector */}
              <div>
                <div className="flex justify-between items-center mb-2">
                  <span className="text-xs font-bold text-gray-900 uppercase tracking-wider">
                    Select Size
                  </span>
                  <button className="text-xs text-gray-500 hover:text-gray-900 underline flex items-center gap-1">
                    <Ruler size={12} />
                    Size Guide
                  </button>
                </div>
                <div className="flex gap-2">
                  {SIZES.map((size) => (
                    <button
                      key={size}
                      type="button"
                      onClick={() => setSelectedSize(size)}
                      className={`w-11 h-10 text-xs font-bold rounded-lg border transition-all cursor-pointer ${
                        selectedSize === size
                          ? 'bg-gray-900 text-white border-gray-900 shadow-xs'
                          : 'bg-white text-gray-700 border-gray-300 hover:border-gray-500'
                      }`}
                    >
                      {size}
                    </button>
                  ))}
                </div>
              </div>

              {/* Color Swatches */}
              <div>
                <span className="text-xs font-bold text-gray-900 uppercase tracking-wider block mb-2">
                  Select Color
                </span>
                <div className="flex items-center gap-2.5">
                  {COLORS.map((col) => {
                    const active = selectedColor === col.hex;
                    return (
                      <button
                        key={col.hex}
                        type="button"
                        onClick={() => setSelectedColor(col.hex)}
                        title={col.name}
                        style={{ backgroundColor: col.hex }}
                        className={`w-8 h-8 rounded-full border-2 transition-all cursor-pointer flex items-center justify-center ${
                          active
                            ? 'border-gray-900 ring-2 ring-gray-400 scale-105'
                            : 'border-white ring-1 ring-gray-300 hover:ring-gray-500'
                        }`}
                      >
                        {active && <Check size={12} className="text-white drop-shadow" />}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Quantity Stepper */}
              <div>
                <span className="text-xs font-bold text-gray-900 uppercase tracking-wider block mb-2">
                  Quantity
                </span>
                <div className="flex items-center border border-gray-300 rounded-lg w-fit overflow-hidden bg-white shadow-2xs">
                  <button
                    onClick={() => setQuantity(q => Math.max(1, q - 1))}
                    className="p-2 hover:bg-gray-100 text-gray-600 transition cursor-pointer"
                    aria-label="Decrease quantity"
                  >
                    <Minus size={14} />
                  </button>
                  <span className="px-5 py-2 text-sm font-bold text-gray-800 border-x border-gray-300 min-w-[50px] text-center select-none">
                    {quantity}
                  </span>
                  <button
                    onClick={() => setQuantity(q => q + 1)}
                    className="p-2 hover:bg-gray-100 text-gray-600 transition cursor-pointer"
                    aria-label="Increase quantity"
                  >
                    <Plus size={14} />
                  </button>
                </div>
              </div>

              {/* CTA Buttons */}
              <div className="pt-2 flex flex-col sm:flex-row gap-3">
                <button
                  type="button"
                  onClick={handleAddToCart}
                  className="flex-1 py-3 px-4 bg-white hover:bg-gray-50 text-gray-900 font-bold text-sm rounded-xl border border-gray-900 transition-colors shadow-2xs flex items-center justify-center gap-2 cursor-pointer"
                >
                  <ShoppingCart size={17} />
                  <span>Add to Cart</span>
                </button>
                <button
                  type="button"
                  onClick={handleBuyNow}
                  className="flex-1 py-3 px-4 bg-gray-900 hover:bg-gray-800 text-white font-bold text-sm rounded-xl transition-colors shadow-xs flex items-center justify-center gap-2 cursor-pointer"
                >
                  <span>Buy Now</span>
                </button>
              </div>

              {/* Trust Badges */}
              <div className="border border-gray-200 rounded-xl p-3.5 bg-gray-50/80 grid grid-cols-3 gap-2 text-center text-xs text-gray-600">
                <div className="flex flex-col items-center gap-1">
                  <RotateCcw size={16} className="text-gray-700" />
                  <span className="font-medium">Free 7-Day Returns</span>
                </div>
                <div className="flex flex-col items-center gap-1 border-x border-gray-200 px-1">
                  <Truck size={16} className="text-gray-700" />
                  <span className="font-medium">Fast Express Delivery</span>
                </div>
                <div className="flex flex-col items-center gap-1">
                  <Ruler size={16} className="text-gray-700" />
                  <span className="font-medium">Perfect Fit Guarantee</span>
                </div>
              </div>

            </div>
          </div>

        </div>

        {/* Customer Reviews Section */}
        <section className="mt-12 bg-white border border-gray-200 rounded-2xl p-6 sm:p-8 shadow-xs">
          <div className="flex items-center justify-between mb-6 pb-4 border-b border-gray-100">
            <div>
              <h2 className="text-base font-bold text-gray-900">
                Customer Reviews ({reviews.length})
              </h2>
              <p className="text-xs text-gray-500 mt-0.5">Verified shoppers who purchased this item</p>
            </div>
            {isLoggedIn ? (
              <button
                onClick={() => setShowReviewForm(!showReviewForm)}
                className="flex items-center gap-1.5 px-3.5 py-2 bg-gray-100 hover:bg-gray-200 text-gray-800 rounded-lg text-xs font-bold transition cursor-pointer"
              >
                <MessageSquarePlus size={14} />
                <span>{showReviewForm ? "Cancel Review" : "Write a Review"}</span>
              </button>
            ) : (
              <div className="flex items-center gap-2.5">
                <span className="text-xs text-gray-500 font-medium">Log in to leave a comment</span>
                <button
                  type="button"
                  onClick={() => {
                    setRedirectAfterLogin({ screen: 'details', params: { productId: product.id } });
                    navigateTo('login');
                  }}
                  className="px-3 py-1.5 bg-gray-900 hover:bg-gray-800 text-white rounded-lg text-xs font-semibold transition cursor-pointer"
                >
                  Log In
                </button>
              </div>
            )}
          </div>

          {/* New Review Form */}
          {isLoggedIn && showReviewForm && (
            <form onSubmit={handleAddReview} className="mb-8 p-5 bg-gray-50 rounded-xl border border-gray-200 space-y-4 animate-in fade-in">
              <p className="text-xs font-bold text-gray-800 uppercase tracking-wider">Leave Your Review</p>
              
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-600">Rating:</span>
                <div className="flex gap-1">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <button
                      key={star}
                      type="button"
                      onClick={() => setNewRating(star)}
                      className="cursor-pointer"
                    >
                      <Star
                        size={18}
                        className={star <= newRating ? 'fill-amber-400 text-amber-500' : 'text-gray-300'}
                      />
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <textarea
                  required
                  rows={3}
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  placeholder="Share details of your experience with size, quality, and fit..."
                  className="w-full p-3 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-gray-900"
                />
              </div>

              <button
                type="submit"
                disabled={submittingReview}
                className="px-4 py-2 bg-gray-900 text-white rounded-lg text-xs font-semibold hover:bg-gray-800 transition cursor-pointer"
              >
                {submittingReview ? "Submitting..." : "Post Review"}
              </button>
            </form>
          )}

          {/* Review List */}
          <div className="space-y-4">
            {reviews.length === 0 ? (
              <p className="text-xs text-gray-400 italic py-2">
                No reviews yet for this product. Be the first to share your thoughts!
              </p>
            ) : (
              reviews.map((rev, idx) => (
                <div key={`rev-${rev.reviewId ?? 'idx'}-${idx}`} className="border-b border-gray-100 pb-4 last:border-b-0 last:pb-0">
                  <div className="flex items-center justify-between mb-1.5">
                    <div className="flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-gray-200 text-gray-700 flex items-center justify-center text-xs font-bold">
                        U{rev.userId}
                      </div>
                      <span className="text-xs font-bold text-gray-800">Shopper #{rev.userId}</span>
                    </div>
                    <span className="text-[11px] text-gray-400">{rev.reviewDate || rev.createdAt || 'Recent'}</span>
                  </div>
                  <div className="flex items-center gap-1 mb-1.5">
                    {[1, 2, 3, 4, 5].map((s) => (
                      <Star
                        key={s}
                        size={12}
                        className={s <= (rev.rating || 5) ? 'fill-amber-400 text-amber-400' : 'text-gray-200'}
                      />
                    ))}
                  </div>
                  <p className="text-xs text-gray-600 leading-relaxed">{rev.comment}</p>
                </div>
              ))
            )}
          </div>
        </section>

        {/* Related Products */}
        <section className="mt-12">
          <div className="flex items-center gap-4 mb-6">
            <h2 className="text-sm font-bold text-gray-900 uppercase tracking-wider">
              You May Also Like
            </h2>
            <div className="flex-1 h-px bg-gray-200" />
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            {related.map((p) => (
              <div
                key={p.id}
                onClick={() => navigateTo('details', { productId: p.id })}
                className="bg-white border border-gray-200 rounded-xl overflow-hidden hover:shadow-md transition cursor-pointer group"
              >
                <div className="h-40 bg-gray-100 overflow-hidden relative">
                  <img
                    src={resolveImageUrl(p.image) || "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80"}
                    alt={p.name}
                    className="w-full h-full object-cover group-hover:scale-105 transition duration-300"
                  />
                </div>
                <div className="p-3">
                  <p className="text-xs font-bold text-gray-800 truncate">{p.name}</p>
                  <p className="text-xs font-bold text-gray-600 mt-0.5">₱{Number(p.price).toFixed(2)}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

      </div>

      <Footer />
    </div>
  );
}
