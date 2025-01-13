// Be Naame Khoda
// FileName: wwwroot/js/main.js

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog').
 */
function loadPage(page) {
    let url = `/${page}`;
    if (page === 'home') {
        url = '/';
    }

    fetch(url)
        .then(response => response.text())
        .then(data => {
            document.getElementById('content').innerHTML = data;
        })
        .catch(error => {
            console.error('Error loading page:', error);
            document.getElementById('content').innerHTML = '<p>Error loading content. Please try again.</p>';
        });
}

// Load the home page by default when the page loads
window.onload = function () {
    loadPage('home');
};