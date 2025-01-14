// Be Naame Khoda
// FileName: wwwroot/js/main.js

$(document).ready(function () {
    // Load the home page by default when the page loads
    loadPage('home');

    // Handle sidebar navigation clicks
    $('#home-link').on('click', function (e) {
        e.preventDefault(); // Prevent default link behavior
        loadPage('home');
    });

    $('#viewlog-link').on('click', function (e) {
        e.preventDefault(); // Prevent default link behavior
        loadPage('viewlog');
    });

    $('#schedule-list-link').on('click', function (e) {
        e.preventDefault(); // Prevent default link behavior
        loadPage('schedule-list');
    });

    // Handle Clear Log button click
    $('#clear-log-link').on('click', function (e) {
        e.preventDefault(); // Prevent default link behavior
        clearLog();
    });
});

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog', 'schedule-list').
 */
function loadPage(page) {
    $.get(`/${page}.html`)
        .done(function (data) {
            // Inject the HTML content into the #content div
            $('#content').html(data);

            // Initialize page-specific functionality
            if (page === 'home') {
                initializeHomePage();
            } else if (page === 'viewlog') {
                initializeLogPolling();
            } else if (page === 'schedule-list') {
                initializeScheduleListPage();
            }
        })
        .fail(function (error) {
            console.error(`Error loading ${page}.html:`, error);
            $('#content').html(`<p>Error loading ${page}. Please try again.</p>`);
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
    $('#date-time').text(now.toLocaleString());
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
    $.get(`/api/prayertimes?year=${year}&month=${month}&day=${day}`)
        .done(function (data) {
            // Update the prayer times in the DOM
            $('#fajr-time').text(data.Fajr || 'N/A');
            $('#dhuhr-time').text(data.Dhuhr || 'N/A');
            $('#asr-time').text(data.Asr || 'N/A');
            $('#maghrib-time').text(data.Maghrib || 'N/A');
            $('#isha-time').text(data.Isha || 'N/A');
        })
        .fail(function (error) {
            console.error('Error fetching prayer times:', error);
            $('#prayer-times-list').html('<li>Error loading prayer times.</li>');
        });
}

/**
 * Initializes log polling for the viewlog page.
 */
function initializeLogPolling() {
    const logContentElement = $('#log-content');
    if (!logContentElement.length) {
        console.error('Log content element not found.');
        return;
    }

    // Function to fetch and update log content
    const fetchLogContent = () => {
        $.get('/api/logs')
            .done(function (data) {
                logContentElement.text(data);
                logContentElement.scrollTop(logContentElement[0].scrollHeight); // Auto-scroll to bottom
            })
            .fail(function (error) {
                console.error('Error fetching log content:', error);
                logContentElement.text('Error loading log content.');
            });
    };

    // Fetch log content immediately
    fetchLogContent();

    // Set up polling to fetch log content every second
    setInterval(fetchLogContent, 1000);
}

/**
 * Initializes the schedule list page.
 */
function initializeScheduleListPage() {
    const scheduleListBody = $('#schedule-list-body');
    const loadingSpinner = $('#loading-spinner');
    const scheduleListContent = $('#schedule-list-content');
    const errorMessage = $('#error-message');

    // Show loading spinner
    loadingSpinner.show();
    scheduleListContent.hide();
    errorMessage.hide();

    // Fetch schedule data
    $.get('/api/schedule-list')
        .done(function (data) {
            // Clear existing rows
            scheduleListBody.empty();

            // Add new rows
            data.forEach(item => {
                const row = `
                    <tr>
                        <td>${item.Playlist}</td>
                        <td>${item.StartTime}</td>
                        <td>${item.EndTime}</td>
                        <td>${item.TriggerEvent}</td>
                        <td>${item.Status}</td>
                    </tr>
                `;
                scheduleListBody.append(row);
            });

            // Show the table
            loadingSpinner.hide();
            scheduleListContent.show();
        })
        .fail(function (error) {
            console.error('Error fetching schedule list:', error);
            loadingSpinner.hide();
            errorMessage.show();
        });
}

/**
 * Clears the log file and reloads the log content.
 */
function clearLog() {
    $.post('/clearlog')
        .done(function () {
            // Reload the log content after clearing
            if ($('#log-content').length) {
                initializeLogPolling();
            }
        })
        .fail(function (error) {
            console.error('Error clearing log:', error);
            $('#content').html('<p>Error clearing log. Please try again.</p>');
        });
}