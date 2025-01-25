      // Be Naame Khoda
// FileName: wwwroot/js/main.js

$(document).ready(function () {
    fetchTimeZone();
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
    $('#events-link').on('click', function (e) { // Changed to events-link
        e.preventDefault();
        loadPage('triggers');
    });

    // Handle Clear Log button click
    $('#clear-log-link').on('click', function (e) {
        e.preventDefault();
        clearLog();
    });
});

let applicationTimeZone = 0;
/**
 * Fetches the timezone offset from server.
 */
function fetchTimeZone() {
    $.get('/api/timezone')
        .done(function (data) {
            applicationTimeZone = data.TimeZoneOffset;
         })
        .fail(function (error) {
           showError('Error fetching time zone.');
      });
}

/**
 * Loads a page dynamically into the #content div using AJAX.
 * @param {string} page - The page to load (e.g., 'home', 'viewlog', 'schedule-list', 'triggers').
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
        initializeLogPage();
    } else if (page === 'schedule-list') {
        initializeScheduleListPage();
    } else if(page === 'triggers') {
        initializeTriggersPage();
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
 * Initializes the log page.
 */
function initializeLogPage() {
    // Fetch log content when the page loads
    fetchLogContent();

    // Handle Refresh button click
    $('#refresh-log-button').on('click', function () {
        fetchLogContent();
    });
}

/**
 * Fetches and displays log content.
 */
function fetchLogContent() {
    showLoadingSpinner();
    $.get('/api/logs')
        .done(function (data) {
            $('#log-content').text(data);
        })
        .fail(function (error) {
            showError('Error fetching log content.');
        })
        .always(function () {
            hideLoadingSpinner();
        });
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
             // Show the schedule list content
            $('#schedule-list-content').show();

            data.forEach(item => {
                const row = `
                    <tr class="${item.Status === 'Playing' ? 'table-primary' : ''}">
                        <td>${item.Name}</td>
                        <td>${formatDateTime(item.StartTime)}</td>
                        <td>${formatDateTime(item.EndTime)}</td>
                        <td>${formatDuration(item.TotalDuration)}</td>
                        <td>${formatDateTime(item.LastPlayTime)}</td>
                         <td>${item.TriggerTime && item.TriggerTime !== "N/A" ? formatDateTime(item.TriggerTime) : 'N/A'}</td>
                        <td>${item.Status}</td>
                          <td>${item.TimeToPlay}</td>
                        <td>
                            <button class="btn btn-sm btn-secondary" onclick="showPlayList('${item.ItemId}')">Playlist</button>
                             <button class="btn btn-sm btn-primary" onclick="reloadItem('${item.ItemId}')">Reload</button>
                        </td>
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
 * Opens the playlist modal for a specific schedule item.
 * @param {string} scheduleItemId - The Id of the schedule item.
 */
function showPlayList(scheduleItemId) {
    showLoadingSpinner();
      $.get(`/api/schedule-list/${scheduleItemId}`)
        .done(function (data) {
                const playlistItemsBody = $('#playlist-items');
                playlistItemsBody.empty();
                 // Show the playlist modal
                 $('#playlist-modal').modal('show');

                if(data && data.length>0) {
                   data.forEach((playlistItem,index )=> {
                        const listItem = `<li class="list-group-item">
                                                ${playlistItem.Path}  ${formatDuration(playlistItem.Duration)}
                                            </li>`;
                        playlistItemsBody.append(listItem);
                   });

               }
              else {
                   playlistItemsBody.append(`<li class="list-group-item" >No items found in playlist</li>`);
               }
        })
        .fail(function (error) {
            showError(`Error fetching playlist for  "${scheduleItemId}".`);
        })
        .always(function () {
             hideLoadingSpinner();
        });
}
/**
 * Reloads a specific schedule item.
 * @param {string} itemId - The ItemId of the schedule item.
 */
function reloadItem(itemId) {
    showLoadingSpinner();
           $.ajax({
                url: '/api/schedule-list',
                type: 'POST',
                contentType: 'application/json',
               data: JSON.stringify({ itemId: itemId}),
                success: function (result) {
                     initializeScheduleListPage();
                    alert("Schedule item reloaded successfully!");
                },
                error: function (error) {
                      showError(`Error reloading schedule item with ID "${itemId}".`);
                }
            })
             .always(function () {
                  hideLoadingSpinner();
            });
        }


/**
 * Initializes the triggers page.
 */
function initializeTriggersPage() {
    fetchTriggers();
}
/**
 * Clears the log file and reloads the log content.
 */
function clearLog() {
    $.post('/clearlog')
        .done(function () {
            // Reload the log content after clearing
            if ($('#log-content').length) {
                fetchLogContent();
            }
        })
        .fail(function (error) {
            showError('Error clearing log.');
        });
}
/**
 * Fetches and displays the triggers list.
 */
function fetchTriggers() {
    showLoadingSpinner();
    $.get('/api/triggers')
        .done(function(data) {
            const eventsListBody = $('#events-list-body');
            eventsListBody.empty();
            //Show the table
            $('#events-list-content').show();
              data.forEach(item => {
                   const formattedTime = formatDate(item.time);
                  const row = `
                    <tr>
                        <td>${item.triggerEvent}</td>
                       <td>${item.type}</td>
                        <td>${formattedTime}</td>
                         <td>
                            ${item.type === 'Manual' ?
                                `<button class="btn btn-sm btn-primary" onclick="editTrigger('${item.triggerEvent}')" >Edit</button>
                                 <button class="btn btn-sm btn-danger"  onclick="deleteTrigger('${item.triggerEvent}')" >Delete</button>`
                                : 'Read-Only'}
                         </td>
                    </tr>
                `;
                eventsListBody.append(row);
              });

        })
        .fail(function (error) {
           showError('Error fetching triggers.');
        })
       .always(function () {
          hideLoadingSpinner();
       });
}
/**
 * Deletes a trigger.
 * @param {string} triggerEventName - The name of the event to delete.
 */
function deleteTrigger(triggerEventName) {
    if (!confirm(`Are you sure you want to delete trigger: ${triggerEventName}?`)) {
        return;
    }
    $.ajax({
        url: '/api/triggers',
        type: 'DELETE',
         contentType: 'application/json',
        data: JSON.stringify({ triggerEvent: triggerEventName }),
         success: function (result) {
           fetchTriggers();
           alert(result);
          },
        error: function (error) {
            showError(`Error deleting trigger "${triggerEventName}".`);
        }
    });
}
/**
 * Opens the trigger editing modal.
 * @param {string} triggerEventName - The name of the event to edit.
 */
function editTrigger(triggerEventName) {
    // Set the event name in the modal
    $('#edit-trigger-name').val(triggerEventName);

   //Fetch the data for the trigger and populate the form

     $.get(`/api/triggers/${triggerEventName}`)
         .done(function (data) {
              // Format the datetime if time exists, else set to null
            const formattedTime = data.time ? formatDateTimeForInput(data.time) : null;
            $('#edit-trigger-time').val(formattedTime);
            $('#edit-trigger-modal').modal('show');

     })
       .fail(function (error) {
           showError('Error fetching event.');
      });
}
/**
 * Opens the new trigger modal
 */
function openNewTriggerModal() {
	const now = new Date();
	//now.setTime(now.getTime() - applicationTimeZone * 60000);
    const year = String(now.getFullYear());
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');

    $('#new-trigger-time').val(`${year}-${month}-${day}T${hours}:${minutes}`);
    $('#new-trigger-modal').modal('show');
}
/**
 * Formats a date-time string for datetime-local input.
 * @param {string} dateTime - The date-time string.
 * @returns {string} - The formatted date-time string.
 */
function formatDateTimeForInput(dateTime) {
     if (!dateTime) return '';
    const date = new Date(dateTime);
    const year = String(date.getFullYear());
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}
/**
 * Submits the new trigger data.
 */
function submitNewTrigger() {
    const triggerEventName = $('#new-trigger-name').val();
    const triggerEventTime = $('#new-trigger-time').val();
      const triggerEventTimeUtc = convertLocalTimeToUtc(triggerEventTime);

    if (!triggerEventName) {
          $('#new-trigger-name-error').show();
        return;
    }
     if (!triggerEventTime) {
          $('#new-trigger-time-error').show();
        return;
    }
    $.ajax({
        url: '/api/triggers',
        type: 'POST',
        contentType: 'application/json',
       data: JSON.stringify({
            triggerEvent: triggerEventName,
            time: triggerEventTimeUtc
        }),
          success: function (result) {
           $('#new-trigger-modal').modal('hide');
           fetchTriggers();
           alert(result);
           $('#new-trigger-name').val('');
           $('#new-trigger-time').val('');
           $('#new-trigger-name-error').hide();
           $('#new-trigger-time-error').hide();
         },
         error: function(error) {
              showError('Error adding new trigger.');
        }
    });
}
/**
 * Submits the edited trigger data.
 */
function submitEditedTrigger() {
    const triggerEventName = $('#edit-trigger-name').val();
    const triggerEventTime = $('#edit-trigger-time').val();
     const triggerEventTimeUtc = convertLocalTimeToUtc(triggerEventTime);
     $.ajax({
        url: '/api/triggers',
        type: 'PUT',
        contentType: 'application/json',
         data: JSON.stringify({
            triggerEvent: triggerEventName,
            time: triggerEventTimeUtc
        }),
        success: function (result) {
             $('#edit-trigger-modal').modal('hide');
           fetchTriggers();
           alert(result);
         },
        error: function(error) {
           showError(`Error updating trigger "${triggerEventName}".`);
        }
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

/**
 * Formats a duration (in milliseconds) into a human-readable string.
 * @param {number} duration - The duration in milliseconds.
 * @returns {string} - The formatted duration (e.g., "01:30:00").
 */
function formatDuration(duration) {
    if (!duration) return 'N/A';
    const seconds = Math.floor(duration / 1000); // Convert milliseconds to seconds
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = seconds % 60;

    return `${pad(hours)}:${pad(minutes)}:${pad(remainingSeconds)}`;
}

/**
 * Formats a date-time string into a human-readable format.
 * @param {string} dateTime - The date-time string (e.g., "2023-10-05T12:00:00").
 * @returns {string} - The formatted date-time (e.g., "2023-10-05 12:00:00").
 */
function formatDateTime(dateTime) {
    if (!dateTime) return 'N/A';
    const date = new Date(dateTime);
    return date.toLocaleString();
}

/**
 * Pads a number with leading zeros.
 * @param {number} num - The number to pad.
 * @returns {string} - The padded number (e.g., "01").
 */
function pad(num) {
    return num.toString().padStart(2, '0');
}

function convertLocalTimeToUtc(timeString) {
    if(!timeString) return null;
    const localTime = new Date(timeString);
    return localTime.toISOString().slice(0, 19);
}

function formatDate(timeString) {
  var date = new Date(timeString);
  var hours = date.getHours();
  var minutes = date.getMinutes();
  var ampm = hours >= 12 ? 'pm' : 'am';
  hours = hours % 12;
  hours = hours ? hours : 12; // the hour '0' should be '12'
  minutes = minutes < 10 ? '0'+minutes : minutes;
  var strTime = hours + ':' + minutes + ' ' + ampm;
  return (date.getMonth()+1) + "/" + date.getDate() + "/" + date.getFullYear() + "  " + strTime;
}
    