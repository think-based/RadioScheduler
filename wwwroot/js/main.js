// Be Naame Khoda
// FileName: wwwroot/js/main.js

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog').
 */
function loadPage(page) {
    let url = `/${page}.html`; // Request /home.html or /viewlog.html
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

            // If the loaded page is home.html, initialize its functionality
            if (page === 'home') {
                initializeHomePage();
            }
            // If the loaded page is viewlog.html, initialize log polling
            else if (page === 'viewlog') {
                initializeLogPolling();
            }
        })
        .catch(error => {
            console.error(`Error loading ${page}.html:`, error);
            document.getElementById('content').innerHTML = `<p>Error loading ${page}. Please try again.</p>`;
        });
}

/**
 * Initializes functionality for the home page.
 */
function initializeHomePage() {
    // Update the current date and time every second
    setInterval(updateDateTime, 1000);

    // Fetch and display prayer times
    fetchPrayerTimes();
}

/**
 * Updates the current date and time.
 */
function updateDateTime() {
    const now = new Date();
    const dateTimeElement = document.getElementById('date-time');
    if (dateTimeElement) {
        dateTimeElement.textContent = now.toLocaleString();
    }
}

/**
 * Fetches and displays prayer times.
 */
function fetchPrayerTimes() {
    const today = new Date();
    const year = today.getFullYear();
    const month = today.getMonth() + 1; // Months are 0-indexed
    const day = today.getDate();

    // Fetch prayer times from the server
    fetch(`/api/prayertimes?year=${year}&month=${month}&day=${day}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch prayer times');
            }
            return response.json();
        })
        .then(data => {
            // Update the prayer times in the DOM
            document.getElementById('fajr-time').textContent = data.Fajr || 'N/A';
            document.getElementById('dhuhr-time').textContent = data.Dhuhr || 'N/A';
            document.getElementById('asr-time').textContent = data.Asr || 'N/A';
            document.getElementById('maghrib-time').textContent = data.Maghrib || 'N/A';
            document.getElementById('isha-time').textContent = data.Isha || 'N/A';
        })
        .catch(error => {
            console.error('Error fetching prayer times:', error);
            document.getElementById('prayer-times-list').innerHTML = '<li>Error loading prayer times.</li>';
        });
}

/**
 * Initializes log polling for the viewlog page.
 */
function initializeLogPolling() {
    const logContentElement = document.getElementById('log-content');
    if (!logContentElement) {
        console.error('Log content element not found.');
        return;
    }

    // Function to fetch and update log content
    const fetchLogContent = () => {
        fetch('/api/logs')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to fetch log content');
                }
                return response.text();
            })
            .then(data => {
                logContentElement.textContent = data;
                logContentElement.scrollTop = logContentElement.scrollHeight; // Auto-scroll to bottom
            })
            .catch(error => {
                console.error('Error fetching log content:', error);
                logContentElement.textContent = 'Error loading log content.';
            });
    };

    // Fetch log content immediately
    fetchLogContent();

    // Set up polling to fetch log content every second
    setInterval(fetchLogContent, 1000);
}

/**
 * Clears the log file and reloads the log content.
 */
function clearLog() {
    fetch('/clearlog', { method: 'POST' })
        .then(response => {
            if (response.ok) {
                // Reload the log content after clearing
                if (window.location.hash === '#viewlog') {
                    initializeLogPolling();
                }
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
    loadPage('home'); // Load /home.html by default
};