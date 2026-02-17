// STAJ2/wwwroot/assets/js/agent-ui.js

let selectedTags = [];
let allAgents = [];

// Filtre menüsünü aç/kapat
window.toggleFilter = () => {
    const dropdown = document.getElementById("tagFilterDropdown");
    if (dropdown) {
        dropdown.style.display = dropdown.style.display === "none" ? "block" : "none";
    }
};

// Etiketleri UsersController üzerinden çek ve checkboxları oluştur
async function loadFilterTags() {
    try {
        const tags = await api.get("/api/Users/tags");
        const container = document.getElementById("tagCheckboxes");
        if (!container) return;

        if (!tags || tags.length === 0) {
            container.innerHTML = '<small class="muted-text">Etiket bulunamadı.</small>';
            return;
        }

        container.innerHTML = tags.map(t => `
            <label style="display:flex; align-items:center; gap:10px; margin-bottom:8px; cursor:pointer; font-size:0.9rem; color:#e5e7eb;">
                <input type="checkbox" value="${t.name}" onchange="handleTagFilter(this)" style="width:16px; height:16px;">
                ${t.name}
            </label>
        `).join("");
    } catch (e) {
        console.error("Etiketler yüklenemedi", e);
        const container = document.getElementById("tagCheckboxes");
        if (container) container.innerHTML = '<small class="text-danger">Etiketler yüklenemedi.</small>';
    }
}

// Seçili etiketleri güncelle ve tabloyu yeniden çiz
window.handleTagFilter = (cb) => {
    if (cb.checked) {
        selectedTags.push(cb.value);
    } else {
        selectedTags = selectedTags.filter(t => t !== cb.value);
    }
    renderTable();
};

async function loadAgents() {
    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);
        allAgents = await res.json();
        renderTable();
    } catch (e) {
        const tbody = document.getElementById("agentRows");
        if (tbody) tbody.innerHTML = `<tr><td colspan="6" class="text-danger">Hata: ${e.message}</td></tr>`;
    }
}

function renderTable() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    // 1. Filtreleme Mantığı
    const filteredAgents = selectedTags.length === 0
        ? allAgents
        : allAgents.filter(a => a.tags && a.tags.some(t => selectedTags.includes(t)));

    if (filteredAgents.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center muted-text" style="padding:20px;">Filtreye uygun cihaz bulunamadı.</td></tr>`;
        return;
    }

    // 2. Tabloyu Doldurma (Özel formatların korunduğu kısım)
    tbody.innerHTML = filteredAgents.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
        const deviceDisplayName = a.displayName || a.machineName || "-";

        // RAM Formatı: %Usage (Total MB)
        const ramInfo = `%${a.ramUsage?.toFixed(1) ?? "0"} (${a.totalRamMb?.toFixed(0) ?? "0"} MB)`;

        // Disk Formatı: Alt alta divler
        let diskCombined = "-";
        if (a.diskUsage && a.totalDiskGb) {
            const usageParts = a.diskUsage.split(' ').filter(x => x.length > 0);
            const sizeParts = a.totalDiskGb.split(' ').filter(x => x.length > 0);
            let disks = [];
            for (let i = 0; i < usageParts.length; i += 2) {
                const label = usageParts[i];
                const usage = usageParts[i + 1];
                const sizeIdx = sizeParts.indexOf(label);
                const size = sizeIdx !== -1 ? sizeParts[sizeIdx + 1] : "?";
                disks.push(`<div style="margin-bottom: 2px;">${label} ${usage} (${parseFloat(size).toFixed(0)} GB)</div>`);
            }
            diskCombined = disks.join('');
        }

        // Cihazın altındaki etiket pilleri
        const tagBadges = (a.tags || []).map(t =>
            `<span class="pill" style="font-size:0.65rem; padding:2px 6px; margin-right:4px; background:rgba(56,189,248,0.1); border:1px solid rgba(56,189,248,0.3); color:#38bdf8; border-radius:4px;">${t}</span>`
        ).join("");

        return `
            <tr>
                <td style="vertical-align: middle;">
                    <strong style="color:#f8fafc; font-size:1rem;">${deviceDisplayName}</strong>
                    <div style="margin-top:6px; display:flex; flex-wrap:wrap; gap:4px;">${tagBadges}</div>
                </td>
                <td style="vertical-align: middle;">${a.ip ?? "-"}</td>
                <td style="vertical-align: middle;">%${a.cpuUsage?.toFixed(1) ?? "0"}</td>
                <td style="vertical-align: middle;">${ramInfo}</td>
                <td style="vertical-align: middle; padding-top: 10px; padding-bottom: 10px;">${diskCombined}</td>
                <td style="vertical-align: middle; color:#9ca3af;">${ts}</td>
            </tr>
        `;
    }).join("");
}

// Başlangıç komutları
loadFilterTags();
loadAgents();
setInterval(loadAgents, 10000);