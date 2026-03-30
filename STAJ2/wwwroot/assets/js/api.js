// STAJ2/wwwroot/assets/js/api.js
(function () {
    // auth.js'teki metodumuzu kullanıyoruz ki her yerde aynı isimle okunsun
    function getToken() {
        if (window.auth && typeof window.auth.getToken === 'function') {
            return window.auth.getToken();
        }
        return localStorage.getItem("staj2_token");
    }

    // Cihazın disklerini ve mevcut eşiklerini getirir
    async function openThresholdSettings(computerId) {
        document.getElementById('modalComputerId').value = computerId;

        const response = await fetch(`/api/Computer/${computerId}/disks`, {
            headers: { 'Authorization': `Bearer ${getToken()}` }
        });

        if (response.status === 403) {
            alert("Dikkat: Sistemdeki yetkileriniz yöneticiler tarafından değiştirildi. Sayfa güncel yetkilerle yeniden yükleniyor...");
            window.location.reload();
            return;
        }

        const disks = await response.json();
        const container = document.getElementById('diskThresholdsContainer');
        container.innerHTML = '<h4>Disk Sınırları</h4>';

        disks.forEach(disk => {
            container.innerHTML += `
            <div class="disk-row">
                <label>${disk.diskName} (${disk.totalSizeGb.toFixed(2)} GB)</label>
                <input type="number" class="disk-threshold-input" 
                       data-name="${disk.diskName}" 
                       value="${disk.thresholdPercent}" min="0" max="100"> %
            </div>
        `;
        });

        document.getElementById('thresholdModal').style.display = 'block';
    }

    // Yeni eşik değerlerini kaydeder
    async function saveThresholds() {
        const computerId = document.getElementById('modalComputerId').value;
        const cpuVal = document.getElementById('cpuThresholdInput').value;
        const ramVal = document.getElementById('ramThresholdInput').value;

        const diskThresholds = [];
        document.querySelectorAll('.disk-threshold-input').forEach(input => {
            diskThresholds.push({
                diskName: input.getAttribute('data-name'),
                thresholdPercent: parseFloat(input.value)
            });
        });

        const body = {
            cpuThreshold: parseFloat(cpuVal),
            ramThreshold: parseFloat(ramVal),
            diskThresholds: diskThresholds
        };

        const response = await fetch(`/api/Computer/update-thresholds/${computerId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getToken()}`
            },
            body: JSON.stringify(body)
        });

        if (response.status === 403) {
            alert("Dikkat: Sistemdeki yetkileriniz yöneticiler tarafından değiştirildi. Sayfa güncel yetkilerle yeniden yükleniyor...");
            window.location.reload();
            return;
        }

        if (response.ok) {
            alert("Eşik değerleri güncellendi!");
            if (typeof closeModal === "function") closeModal();
        } else {
            alert("Hata oluştu.");
        }
    }

    // Sistemin genel Fetch Wrapper'ı
    async function request(path, options = {}) {
        const base = window.APP_CONFIG?.API_BASE ?? "";
        const url = base + path;

        const headers = options.headers ? { ...options.headers } : {};
        if (!headers["Content-Type"] && options.body) headers["Content-Type"] = "application/json";

        const token = getToken();
        if (token) headers["Authorization"] = `Bearer ${token}`;

        const res = await fetch(url, { ...options, headers });

        // YENİ EKLENEN KISIM: 401 Unauthorized (Token Süresi Dolmuş/Geçersiz) Kontrolü
        if (res.status === 401) {
            alert("Oturum süreniz doldu veya geçersiz. Lütfen tekrar giriş yapın.");
            if (window.auth && typeof window.auth.clearAuth === 'function') {
                window.auth.clearAuth(); // Token'ı temizle
            } else {
                localStorage.removeItem("staj2_token");
            }
            window.location.href = "/login.html?reason=expired";
            throw new Error("Oturum süresi doldu (401).");
        }

        if (res.status === 403) {
            alert("Dikkat: Sistemdeki yetkileriniz yöneticiler tarafından değiştirildi. Sayfa güncel yetkilerle yeniden yükleniyor...");
            window.location.reload();
            throw new Error("Yetkiler değiştirildiği için işlem iptal edildi.");
        }

        const contentType = res.headers.get("content-type") || "";
        const isJson = contentType.includes("application/json");
        const data = isJson ? await res.json().catch(() => null) : await res.text().catch(() => "");

        if (!res.ok) {
            const msg = (typeof data === "string" && data) ? data
                : (data && data.errorMessage) ? data.errorMessage
                    : (data && data.message) ? data.message
                        : `Sistem Hatası (HTTP ${res.status})`;
            throw new Error(msg);
        }
        return data;
    }
    /*
    // --- YENİ EKLENEN KISIM: SESSİZ ARKA PLAN DEVRİYESİ ---
    function startSilentPermissionPolling() {
        // Eğer kullanıcı giriş yapmamışsa (token yoksa) hiç başlatma
        if (!getToken()) return;

        // Her 45 saniyede bir arka planda sessizce çalışır
        setInterval(async () => {
            try {
                const token = getToken();
                if (!token) return;

                // Kendi wrapper'ımızı (request) kullanmıyoruz çünkü hata verip ekrana yansımasını istemiyoruz.
                // Saf (raw) fetch ile sessizce soruyoruz.
                const response = await fetch('/api/Auth/my-permissions', {
                    headers: { 'Authorization': `Bearer ${token}` }
                });

                // Eğer sunucudan düzgün cevap gelmediyse (örn: token süresi dolduysa) sessizce çık, 
                // ana sistem zaten başka bir işlemde kullanıcıyı login'e atacaktır.
                if (!response.ok) return;

                const newPermissions = await response.json();

                // LocalStorage'daki eski yetkileri al
                const oldPermissionsRaw = localStorage.getItem("staj2_permissions");
                const oldPermissions = oldPermissionsRaw ? JSON.parse(oldPermissionsRaw) : [];

                // Eski yetkiler ile Yeni yetkileri karşılaştır
                // 1. Uzunlukları farklıysa KESİN değişmiştir
                // 2. Uzunlukları aynı olsa bile içerikleri farklı olabilir (örn: biri silinip diğeri eklendiyse)
                const isDifferent =
                    newPermissions.length !== oldPermissions.length ||
                    !newPermissions.every(perm => oldPermissions.includes(perm));

                // Eğer veritabanındaki yetkilerle bizim hafızadakiler eşleşmiyorsa aksiyon al!
                if (isDifferent) {
                    // Hafızayı güncelle
                    localStorage.setItem("staj2_permissions", JSON.stringify(newPermissions));

                    // Kullanıcıya haber ver ve sayfayı yenileterek yeni butonların/menülerin gelmesini sağla
                    alert("Hesap yetkileriniz yöneticiler tarafından güncellendi. Yeni özelliklerin aktif olması için sayfa yenileniyor...");
                    window.location.reload();
                }

            } catch (error) {
                // İnternet anlık koparsa vs. konsolu kırmızıya boyama, sessizce hatayı yut
            }
        }, 45000); // 45.000 milisaniye = 45 Saniye
    }

    // Devriyeyi başlat
    startSilentPermissionPolling();
    // -----------------------------------------------------
    */
    window.api = {
        get: (path) => request(path),
        post: (path, body) => request(path, { method: "POST", body: JSON.stringify(body) }),
        put: (path, body) => request(path, { method: "PUT", body: JSON.stringify(body) }),
        del: (path) => request(path, { method: "DELETE" }),
        openThresholdSettings: openThresholdSettings,
        saveThresholds: saveThresholds
    };
})();