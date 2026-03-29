const CART_KEY = 'amrod_cart'

function getCart() {
	try {
		return JSON.parse(localStorage.getItem(CART_KEY)) || []
	} catch {
		return []
	}
}

function saveCart(cart) {
	localStorage.setItem(CART_KEY, JSON.stringify(cart))
	updateCartCount()
}

function addToCart(product, quantity) {
	const cart = getCart()
	const existing = cart.find((i) => i.id === product.id)
	if (existing) {
		existing.quantity += quantity
	} else {
		// Only store what we need — avoids description breaking JSON
		cart.push({
			id: product.id,
			name: product.name,
			price: product.price,
			stock: product.stock,
			imageUrl: product.imageUrl,
			quantity,
		})
	}
	saveCart(cart)
	showCartToast(product.name)
}

function removeFromCart(productId) {
	saveCart(getCart().filter((i) => i.id !== productId))
}

function updateQuantity(productId, quantity) {
	const cart = getCart()
	const item = cart.find((i) => i.id === productId)
	if (item) {
		item.quantity = Math.max(1, quantity)
		saveCart(cart)
	}
}

function clearCart() {
	localStorage.removeItem(CART_KEY)
	updateCartCount()
}

function getCartTotal() {
	return getCart().reduce((sum, i) => sum + i.price * i.quantity, 0)
}

function updateCartCount() {
	const count = getCart().reduce((sum, i) => sum + i.quantity, 0)
	document.querySelectorAll('#cart-count').forEach((el) => {
		el.textContent = count
		el.style.display = count > 0 ? 'inline' : 'none'
	})
}

function formatPrice(amount) {
	return `R${Number(amount).toFixed(2)}`
}

function showCartToast(name) {
	const toast = document.createElement('div')
	toast.className = 'cart-toast'
	toast.textContent = `"${name}" added to cart`
	document.body.appendChild(toast)
	setTimeout(() => toast.classList.add('show'), 10)
	setTimeout(() => {
		toast.classList.remove('show')
		setTimeout(() => toast.remove(), 300)
	}, 2500)
}

// Add toast styles dynamically
const toastStyle = document.createElement('style')
toastStyle.textContent = `
.cart-toast { position: fixed; bottom: 1.5rem; right: 1.5rem; background: #25bff1; color: #1a1a1a; padding: 0.75rem 1.25rem; border-radius: 8px; font-size: 0.9rem; transform: translateY(20px); opacity: 0; transition: all 0.3s; z-index: 999; }
.cart-toast.show { transform: translateY(0); opacity: 1; }
`
document.head.appendChild(toastStyle)

document.addEventListener('DOMContentLoaded', updateCartCount)
