document.addEventListener('DOMContentLoaded', renderCart)

function renderCart() {
	const cart = getCart()
	const itemsEl = document.getElementById('cart-items')
	const emptyEl = document.getElementById('cart-empty')
	const summaryEl = document.getElementById('cart-summary')

	// Clear all existing cart item elements first
	itemsEl.querySelectorAll('.cart-item').forEach((el) => el.remove())

	if (cart.length === 0) {
		emptyEl.classList.remove('hidden')
		summaryEl.style.display = 'none'
		return
	}

	emptyEl.classList.add('hidden')
	summaryEl.style.display = ''

	cart.forEach((item) => {
		const el = document.createElement('div')
		el.className = 'cart-item'
		el.innerHTML = `
            ${
							item.imageUrl
								? `<img class="cart-item-img" src="${item.imageUrl}" alt="${item.name}" />`
								: `<div class="cart-item-img" style="display:flex;align-items:center;justify-content:center;font-size:2rem;background:var(--bg)">🎁</div>`
						}
            <div>
                <div class="cart-item-name">${item.name}</div>
                <div class="cart-item-price">${formatPrice(item.price)} each</div>
            </div>
            <div class="cart-item-actions">
                <strong>${formatPrice(item.price * item.quantity)}</strong>
                <div style="display:flex;align-items:center;gap:0.4rem">
                    <button class="btn btn-outline btn-sm" onclick="changeQty('${item.id}', ${item.quantity - 1})">−</button>
                    <span>${item.quantity}</span>
                    <button class="btn btn-outline btn-sm" onclick="changeQty('${item.id}', ${item.quantity + 1})">+</button>
                </div>
                <button class="btn btn-outline btn-sm" onclick="removeItem('${item.id}')" aria-label="Remove ${item.name}">Remove</button>
            </div>`
		itemsEl.appendChild(el)
	})

	const total = getCartTotal()
	document.getElementById('summary-subtotal').textContent = formatPrice(total)
	document.getElementById('summary-total').textContent = formatPrice(total)
}

function changeQty(productId, newQty) {
	if (newQty < 1) {
		removeItem(productId)
		return
	}
	updateQuantity(productId, newQty)
	renderCart()
}

function removeItem(productId) {
	removeFromCart(productId)
	renderCart()
}

async function checkout() {
	const firstName = document.getElementById('first-name').value.trim()
	const lastName = document.getElementById('last-name').value.trim()
	const email = document.getElementById('email').value.trim()
	const phone = document.getElementById('phone').value.trim()
	const notes = document.getElementById('notes').value.trim()
	const errorEl = document.getElementById('form-error')
	const btn = document.getElementById('checkout-btn')

	errorEl.classList.add('hidden')

	if (!firstName) {
		showFormError('First name is required.')
		return
	}
	if (!lastName) {
		showFormError('Last name is required.')
		return
	}
	if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
		showFormError('A valid email is required.')
		return
	}

	const cart = getCart()
	if (cart.length === 0) {
		showFormError('Your cart is empty.')
		return
	}

	btn.disabled = true
	btn.textContent = 'Processing...'

	try {
		// create or fetch customer
		const customerPayload = {
			firstName: firstName,
			lastName: lastName,
			email: email,
			phone: phone || '',
		}

		console.log('Customer payload:', JSON.stringify(customerPayload))

		const customerRes = await fetch('/api/customers', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(customerPayload),
		})

		if (!customerRes.ok) {
			const err = await customerRes.json()
			throw new Error(err.message || 'Failed to save customer details.')
		}

		const customer = await customerRes.json()

		// place order
		const orderPayload = {
			customerId: customer.id,
			items: cart.map((i) => ({ productId: i.id, quantity: i.quantity })),
			notes: notes || '',
		}

		console.log('Order payload:', JSON.stringify(orderPayload))

		const orderRes = await fetch('/api/orders', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(orderPayload),
		})

		if (!orderRes.ok) {
			const err = await orderRes.json()
			throw new Error(err.message || 'Failed to place order.')
		}

		const order = await orderRes.json()

		// initiate PayFast payment
		const pfRes = await fetch(`/api/payfast/initiate/${order.id}`)
		if (!pfRes.ok) throw new Error('Failed to initiate payment.')

		const { paymentUrl, formData } = await pfRes.json()

		// Build and submit form to PayFast
		clearCart()
		submitPayFastForm(paymentUrl, formData)
	} catch (err) {
		showFormError(err.message)
		btn.disabled = false
		btn.textContent = 'Proceed to Payment'
	}

	function showFormError(msg) {
		errorEl.textContent = msg
		errorEl.classList.remove('hidden')
	}
}

function submitPayFastForm(paymentUrl, formData) {
	const form = document.createElement('form')
	form.method = 'POST'
	form.action = paymentUrl

	Object.entries(formData).forEach(([key, value]) => {
		const input = document.createElement('input')
		input.type = 'hidden'
		input.name = key
		input.value = value
		form.appendChild(input)
	})

	document.body.appendChild(form)
	form.submit()
}
