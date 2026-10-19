document.querySelectorAll('[data-image]').forEach(button => button.addEventListener('click', () => {
    const target = document.getElementById('main-product-image');
    if (target) target.src = button.dataset.image;
}));
document.querySelectorAll('form[data-confirm]').forEach(form => form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
document.querySelectorAll('img').forEach(image => image.addEventListener('error', () => {
    if (!image.src.endsWith('/images/products/placeholder.svg')) image.src = '/images/products/placeholder.svg';
}, { once: true }));
