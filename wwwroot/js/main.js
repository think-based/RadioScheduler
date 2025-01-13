// Be Naame Khoda
// FileName: wwwroot/js/main.js

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog').
 */
function loadPage(page) {
    let url = `/${page}.html`; // Load the corresponding HTML file
    fetch(url)
        .then(response => response.text())
        .then(data => {
            // Inject the HTML content into the #content div
            document.getElementById('content').innerHTML = data;

            // If the loaded page is viewlog.html, fetch and display the log content
            if (page === 'viewlog') {
                fetchLogContent();
            }
        })
        .catch(error => {
            console.error('Error loading page:', error);
            document.getElementById('content').innerHTML = '<p>Error loading content. Please try again.</p>';
        });
}

/**
 * Fetches and displays the log content.
 */
function fetchLogContent() {
    fetch('/api/logs')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch log content');
            }
            return response.text();
        })
        .then(data => {
            document.getElementById('log-content').textContent = data;
        })
        .catch(error => {
            console.error('Error fetching log content:', error);
            document.getElementById('log-content').textContent = 'Error loading log content.';
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