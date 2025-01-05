function clearLog() {
    fetch('/clearlog', { method: 'GET' })
        .then(response => {
            if (response.ok) {
                alert('لاگ‌ها با موفقیت پاک شدند.');
                window.location.reload();
            } else {
                alert('خطا در پاک کردن لاگ‌ها.');
            }
        });
}

// به‌روزرسانی زمان فعلی
function updateTime() {
    const now = new Date();
    document.getElementById('gregorian-time').textContent = now.toLocaleString('en-US');
    document.getElementById('persian-time').textContent = new Intl.DateTimeFormat('fa-IR').format(now);
    document.getElementById('hijri-time').textContent = new Intl.DateTimeFormat('ar-SA').format(now);
}

setInterval(updateTime, 1000);
updateTime();