window.haytPwa = window.haytPwa || {};

(function () {
    let deferredPrompt = null;

    window.addEventListener('beforeinstallprompt', function (event) {
        event.preventDefault();
        deferredPrompt = event;
        console.info('Hayt PWA install prompt captured.');
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        console.info('Hayt PWA installed.');
    });

    window.haytPwa.tryInstall = async function () {
        if (!deferredPrompt) {
            return 'در حال حاضر نصب مستقیم توسط مرورگر فعال نیست. از منوی مرورگر گزینه Install app یا Add to Home Screen را انتخاب کنید.';
        }

        deferredPrompt.prompt();

        const choice = await deferredPrompt.userChoice;
        deferredPrompt = null;

        if (choice && choice.outcome === 'accepted') {
            return 'درخواست نصب پذیرفته شد.';
        }

        return 'درخواست نصب لغو شد.';
    };
})();
