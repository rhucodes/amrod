let debounceTimer
let currentProducts = []

async function fetchProducts() {
	const search = document.getElementById('search').value.trim()
	const minPrice = document.getElementById('min-price').value
	const maxPrice = document.getElementById('max-price').value
	const inStock = document.getElementById('in-stock').checked
	const sortVal = document.getElementById('sort-by').value
	const errorEl = document.getElementById('filter-error')

	// Validate price inputs
	errorEl.textContent = ''
	if (minPrice && isNaN(Number(minPrice))) {
		errorEl.textContent = 'Min price must be a number.'
		return
	}
	if (maxPrice && isNaN(Number(maxPrice))) {
		errorEl.textContent = 'Max price must be a number.'
		return
	}
	if (minPrice && maxPrice && Number(minPrice) > Number(maxPrice)) {
		errorEl.textContent = 'Min price cannot exceed max price.'
		return
	}

	const [sortBy, sortOrder] = sortVal ? sortVal.split('_') : ['', '']

	const params = new URLSearchParams()
	if (search) params.set('search', search)
	if (minPrice) params.set('minPrice', minPrice)
	if (maxPrice) params.set('maxPrice', maxPrice)
	if (inStock) params.set('inStock', 'true')
	if (sortBy) params.set('sortBy', sortBy)
	if (sortOrder) params.set('sortOrder', sortOrder)

	showLoading(true)

	try {
		const res = await fetch(`/api/products?${params}`)
		if (!res.ok) throw new Error('Failed to load products.')
		currentProducts = await res.json()
		renderProducts(currentProducts)
	} catch (err) {
		errorEl.textContent = err.message
		renderProducts([])
	} finally {
		showLoading(false)
	}
}

function renderProducts(products) {
	const grid = document.getElementById('product-grid')
	const empty = document.getElementById('empty-state')

	grid.innerHTML = ''

	if (products.length === 0) {
		empty.classList.remove('hidden')
		return
	}

	empty.classList.add('hidden')

	products.forEach((p, index) => {
		const card = document.createElement('article')
		card.className = 'product-card'
		card.tabIndex = 0
		card.setAttribute('role', 'button')
		card.setAttribute('aria-label', `View details for ${p.name}`)

		const outOfStock = p.stock === 0
		const imgHtml = p.imageUrl
			? `<img class="product-card-img" src="${p.imageUrl}" alt="${p.name}" loading="${index < 8 ? 'eager' : 'lazy'}" />`
			: `<div class="product-card-img placeholder" aria-hidden="true">🎁</div>`

		card.innerHTML = `
            ${imgHtml}
            <div class="product-card-body">
                <div class="product-card-name">${p.name}</div>
                <div class="product-card-price">${p.price === 0 ? 'POA' : formatPrice(p.price)}</div>
                <div class="product-card-stock ${outOfStock ? 'out' : ''}">
                    ${outOfStock ? 'Out of stock' : `${p.stock} in stock`}
                </div>
            </div>`

		card.addEventListener('click', () => openModal(p))
		card.addEventListener('keydown', (e) => {
			if (e.key === 'Enter' || e.key === ' ') openModal(p)
		})

		grid.appendChild(card)
	})
}

function openModal(product) {
	const overlay = document.getElementById('modal-overlay')
	const body = document.getElementById('modal-body')
	const outOfStock = product.stock === 0

	body.innerHTML = `
        ${
					product.imageUrl
						? `<img class="modal-img" src="${product.imageUrl}" alt="${product.name}" />`
						: `<div class="modal-img placeholder" aria-hidden="true" style="display:flex;align-items:center;justify-content:center;font-size:4rem">🎁</div>`
				}
        <h2 class="modal-name" id="modal-title">${product.name}</h2>
        <div class="modal-price">${product.price === 0 ? 'Price on Application' : formatPrice(product.price)}</div>
        <p class="modal-desc">${product.description}</p>
        <div class="modal-stock">
            ${
							outOfStock
								? '<span style="color:var(--accent)">Out of stock</span>'
								: `<span style="color:#2d8a4e">&#10003; In stock</span> &mdash; ${product.stock} available`
						}
        </div>
        ${
					!outOfStock && product.price > 0
						? `
        <div class="modal-actions">
            <input type="number" class="input qty-input" id="modal-qty" value="1" min="1" max="${product.stock}" aria-label="Quantity" />
            <button class="btn btn-primary" onclick="modalAddToCart(${JSON.stringify(product).replace(/"/g, '&quot;')})">
                Add to Cart
            </button>
        </div>`
						: ''
				}`

	overlay.classList.remove('hidden')
	document.body.style.overflow = 'hidden'
}

function closeModal() {
	document.getElementById('modal-overlay').classList.add('hidden')
	document.body.style.overflow = ''
}

function modalAddToCart(product) {
	const qty = parseInt(document.getElementById('modal-qty').value, 10)
	if (isNaN(qty) || qty < 1) return
	addToCart(product, qty)
	closeModal()
}

function clearFilters() {
	document.getElementById('search').value = ''
	document.getElementById('min-price').value = ''
	document.getElementById('max-price').value = ''
	document.getElementById('in-stock').checked = false
	document.getElementById('sort-by').value = ''
	fetchProducts()
}

function showLoading(show) {
	document.getElementById('loading').classList.toggle('hidden', !show)
	document.getElementById('product-grid').classList.toggle('hidden', show)
}

// Debounced input handlers
;['search', 'min-price', 'max-price'].forEach((id) => {
	document.getElementById(id)?.addEventListener('input', () => {
		clearTimeout(debounceTimer)
		debounceTimer = setTimeout(fetchProducts, 400)
	})
})
;['in-stock', 'sort-by'].forEach((id) => {
	document.getElementById(id)?.addEventListener('change', fetchProducts)
})

// Modal close handlers
document.getElementById('modal-close')?.addEventListener('click', closeModal)
document.getElementById('modal-overlay')?.addEventListener('click', (e) => {
	if (e.target === e.currentTarget) closeModal()
})
document.addEventListener('keydown', (e) => {
	if (e.key === 'Escape') closeModal()
})

// Initial load
fetchProducts()
