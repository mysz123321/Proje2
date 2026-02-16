// STAJ2/wwwroot/assets/js/api.js
(function () {
    function getToken() {
        // Auth.js veya Login.js ile tutarlı olduğundan emin olun
        return localStorage.getItem("staj2_token");
    }
    // Cihazın disklerini ve mevcut eşiklerini getirir
    async function openThresholdSettings(computerId) {
        document.getElementById('modalComputerId').value = computerId;

        // API'den disk bilgilerini çek (Yeni yazdığımız GetComputerDisks endpoint'i)
        const response = await fetch(`/api/Admin/computers/${computerId}/disks`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
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

        const response = await fetch(`/api/Admin/update-thresholds/${computerId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(body)
        });

        if (response.ok) {
            alert("Eşik değerleri güncellendi!");
            closeModal();
        } else {
            alert("Hata oluştu.");
        }
    }
    async function request(path, options = {}) {
        const base = window.APP_CONFIG?.API_BASE ?? "";
        const url = base + path;

        const headers = options.headers ? { ...options.headers } : {};
        if (!headers["Content-Type"] && options.body) headers["Content-Type"] = "application/json";

        const token = getToken();
        if (token) headers["Authorization"] = `Bearer ${token}`;

        const res = await fetch(url, { ...options, headers });

        const contentType = res.headers.get("content-type") || "";
        const isJson = contentType.includes("application/json");
        const data = isJson ? await res.json().catch(() => null) : await res.text().catch(() => "");

        if (!res.ok) {
            const msg = (typeof data === "string" && data) ? data
                : (data && data.message) ? data.message
                    : `HTTP ${res.status}`;
            throw new Error(msg);
        }
        return data;
    }

    // Bu nesnenin içinde 'put' anahtarı MUTLAKA olmalı
    window.api = {
        get: (path) => request(path),
        post: (path, body) => request(path, { method: "POST", body: JSON.stringify(body) }),
        put: (path, body) => request(path, { method: "PUT", body: JSON.stringify(body) }),
        del: (path) => request(path, { method: "DELETE" })
    };
})();