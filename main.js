// Be Naame Khoda
// FileName: main.js

$(document).ready(function () {
    // Load the home page by default
    loadPage('home');

    // Handle sidebar navigation clicks
    $('#home-link').on('click', function (e) {
        e.preventDefault();
        loadPage('home');
    });

    $('#viewlog-link').on('click', function (e) {
        e.preventDefault();
        loadPage('viewlog');
    });

    $('#schedule-list-link').on('click', function (e) {
        e.preventDefault();
        loadPage('schedule-list');
    });

    // Handle Clear Log button click
    $('#clear-log-link').on('click', function (e) {
        e.preventDefault();
        clearLog();
    });
});

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog', 'schedule-list').
 */
function loadPage(page) {
    showLoadingSpinner();
    $.get(`/${page}.html`)
        .done(function (data) {
            $('#content').html(data);
            initializePage(page); // Initialize page-specific functionality
        })
        .fail(function (error) {
            showError(`Error loading ${page}.`);
        })
        .always(function () {
            hideLoadingSpinner();
        });
}

/**
 * Initializes page-specific functionality.
 * @param {string} page - The page being initialized.
 */
function initializePage(page) {
    if (page === 'home') {
        initializeHomePage();
    } else if (page === 'viewlog') {
        initializeLogPolling();
    } else if (page === 'schedule-list') {
        initializeScheduleListPage();
    }
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
    const month = today.getMonth() + 1;
    const day = today.getDate();

    $.get(`/api/prayertimes?year=${year}&month=${month}&day=${day}`)
        .done(function (data) {
            $('#fajr-time').text(data.Fajr || 'N/A');
            $('#dhuhr-time').text(data.Dhuhr || 'N/A');
            $('#asr-time').text(data.Asr || 'N/A');
            $('#maghrib-time').text(data.Maghrib || 'N/A');
            $('#isha-time').text(data.Isha || 'N/A');
        })
        .fail(function (error) {
            showError('Error fetching prayer times.');
        });
}

/**
 * Initializes real-time log updates using Server-Sent Events (SSE).
 */
function initializeLogPolling() {
    const logContentElement = $('#log-content');
    if (!logContentElement.length) return;

    const eventSource = new EventSource('/api/logs/stream');
    eventSource.onmessage = function (event) {
        logContentElement.text(event.data);
        logContentElement.scrollTop(logContentElement[0].scrollHeight); // Auto-scroll to bottom
    };

    eventSource.onerror = function () {
        logContentElement.text('Error connecting to log stream.');
    };
}

/**
 * Initializes the schedule list page.
 */
function initializeScheduleListPage() {
    showLoadingSpinner();
    $.get('/api/schedule-list')
        .done(function (data) {
            const scheduleListBody = $('#schedule-list-body');
            scheduleListBody.empty();

            data.forEach(item => {
                const row = `
                    <tr>
                        <td>${item.Name}</td>
                        <td>${item.StartTime}</td>
                        <td>${item.EndTime}</td>
                        <td>${item.TriggerEvent}</td>
                        <td>${item.Status}</td>
                    </tr>
                `;
                scheduleListBody.append(row);
            });
        })
        .fail(function (error) {
            showError('Error fetching schedule list.');
        })
        .always(function () {
            hideLoadingSpinner();
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
            showError('Error clearing log.');
        });
}

/**
 * Shows the loading spinner.
 */
function showLoadingSpinner() {
    $('#loading-spinner').show();
}

/**
 * Hides the loading spinner.
 */
function hideLoadingSpinner() {
    $('#loading-spinner').hide();
}

/**
 * Displays an error message in the UI.
 * @param {string} message - The error message to display.
 */
function showError(message) {
    $('#error-message').text(message).show();
}

/**
 * Hides the error message in the UI.
 */
function hideError() {
    $('#error-message').hide();
}