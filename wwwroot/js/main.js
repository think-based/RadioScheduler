// Be Naame Khoda
// FileName: wwwroot/js/main.js

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog').
 */
function loadPage(page) {
    let url = `/${page}.html`; // Load the corresponding HTML file
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Failed to load ${page}.html`);
            }
            return response.text();
        })
        .then(data => {
            // Inject the HTML content into the #content div
            document.getElementById('content').innerHTML = data;

            // If the loaded page is viewlog.html, fetch and display the log content
            if (page === 'viewlog') {
                fetchLogContent();
            }
        })
        .catch(error => {
            console.error(`Error loading ${page}.html:`, error);
            document.getElementById('content').innerHTML = `<p>Error loading ${page}. Please try again.</p>`;
        });
}

/**
 * Fetches and displays the log content.
 */
function fetchLogContent() {
    const logContentElement = document.getElementById('log-content');
    if (!logContentElement) {
        console.error('Log content element not found.');
        return;
    }

    fetch('/api/logs')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch log content');
            }
            return response.text();
        })
        .then(data => {
            logContentElement.textContent = data;
        })
        .catch(error => {
            console.error('Error fetching log content:', error);
            logContentElement.textContent = 'Error loading log content.';
        });
}

/**
 * Clears the log file and reloads the log content.
 */
function clearLog() {
    fetch('/clearlog', { method: 'POST' })
        .then(response => {
            if (response.ok) {
                // Reload the log content after clearing
                fetchLogContent();
            } else {
                console.error('Error clearing log:', response.statusText);
            }
        })
        .catch(error => {
            console.error('Error clearing log:', error);
        });
}

// Load the home page by default when the page loads
window.onload = function () {
    loadPage('home');
};