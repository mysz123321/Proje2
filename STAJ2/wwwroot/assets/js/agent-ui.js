// STAJ2/wwwroot/assets/js/agent-ui.js

let selectedTags = [];
let allAgents = [];

// Filtrele butonuna basınca çalışacak fonksiyon
window.applyFilter = () => {
    // Select2'deki seçili değerleri alıyoruz
    selectedTags = $('#tagSelect').val() || [];
    renderTable(); // Tabloyu "VE" mantığıyla yeniden çiz
};

// Etiketleri Select2 içine doldurma
async function loadFilterTags() {
    try {
        const tags = await api.get("/api/Users/tags");
        const select = document.getElementById("tagSelect");
        if (!select) return;

        tags.forEach(t => {
            const option = document.createElement("option");
            option.value = t.name;
            option.textContent = t.name;
            select.appendChild(option);
        });

        // Veriler dolduktan sonra Select2'yi tazele
        $('#tagSelect').trigger('change');
    } catch (e) { console.error("Etiket yükleme hatası", e); }
}

async function loadAgents() {
    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);
        allAgents = await res.json();
        renderTable(); // Tabloyu çiz (Filtre varsa ona göre çizer)
    } catch (e) {
        const tbody = document.getElementById("agentRows");
        if (tbody) tbody.innerHTML = `<tr><td colspan="6" class="text-danger">Hata: ${e.message}</td></tr>`;
    }
}

function renderTable() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    // --- "VE" (AND) MANTIĞI: Seçilen tüm etiketlerin cihazda olması lazım ---
    const filteredAgents = selectedTags.length === 0
        ? allAgents
        : allAgents.filter(a => selectedTags.every(t => a.tags && a.tags.includes(t)));

    if (filteredAgents.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center muted-text" style="padding:20px;">Seçilen tüm kriterlere uyan cihaz bulunamadı.</td></tr>`;
        return;
    }

    tbody.innerHTML = filteredAgents.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
        const deviceDisplayName = a.displayName || a.machineName || "-";
        const ramInfo = `%${a.ramUsage?.toFixed(1) ?? "0"} (${a.totalRamMb?.toFixed(0) ?? "0"} MB)`;

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

        const tagBadges = (a.tags || []).map(t => `<span class="pill" style="font-size:0.65rem; padding:2px 6px; margin-right:4px; background:rgba(56,189,248,0.1); border:1px solid rgba(56,189,248,0.3); color:#38bdf8; border-radius:4px;">${t}</span>`).join("");

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

// Init
loadFilterTags();
loadAgents();
setInterval(loadAgents, 10000); // Otomatik yenileme devam ediyor ama filtreyi bozmuyor