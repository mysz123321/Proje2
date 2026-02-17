// STAJ2/wwwroot/assets/js/agent-ui.js

let selectedTags = [];
let allAgents = [];

// --- 1. ANA TABLO VE FİLTRELEME ---

window.applyFilter = () => {
    selectedTags = $('#tagSelect').val() || [];
    renderTable();
};

async function loadFilterTags() {
    try {
        // Not: Etiketleri herkes çekebilsin diye UsersController'a yönlendirdik
        const tags = await api.get("/api/Users/tags");
        const $select = $('#tagSelect');
        $select.empty();
        tags.forEach(t => { $select.append(new Option(t.name, t.name, false, false)); });
        $select.trigger('change');
    } catch (e) { console.error("Filtre etiketleri yüklenemedi", e); }
}

async function loadAgents() {
    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        allAgents = await res.json();
        renderTable();
    } catch (e) { console.error(e); }
}

function renderTable() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;
    const isAdmin = auth.hasRole("Yönetici");

    const filtered = selectedTags.length === 0
        ? allAgents
        : allAgents.filter(a => selectedTags.every(t => a.tags && a.tags.includes(t)));

    tbody.innerHTML = filtered.map(a => {
        const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
        const tags = (a.tags || []).map(t => `<span class="pill" style="font-size:0.65rem;">${t}</span>`).join("");

        const actionButtons = isAdmin ? `
            <div style="display:flex; gap:5px;">
                <button class="btn primary border small" onclick="handleRename(${a.computerId}, '${a.displayName || a.machineName}')" title="İsim Değiştir">✏️</button>
                <button class="btn warning border small" onclick="openThresholdSettings(${a.computerId})" title="Ayarlar">⚙️</button>
            </div>` : "";

        return `<tr>
            <td style="vertical-align:middle;"><strong>${a.displayName || a.machineName}</strong><br/>${tags}</td>
            <td style="vertical-align:middle;">${a.ip || "-"}</td>
            <td style="vertical-align:middle;">%${a.cpuUsage?.toFixed(1) ?? "0"}</td>
            <td style="vertical-align:middle;">%${a.ramUsage?.toFixed(1) ?? "0"}</td>
            <td style="vertical-align:middle;">${formatDisks(a)}</td>
            <td style="vertical-align:middle; font-size:0.8rem; color:#9ca3af;">${ts}</td>
            ${isAdmin ? `<td style="vertical-align:middle;">${actionButtons}</td>` : ""}
        </tr>`;
    }).join("");
}

function formatDisks(a) {
    if (!a.diskUsage || !a.totalDiskGb) return "-";
    const u = a.diskUsage.split(' ').filter(x => x);
    const s = a.totalDiskGb.split(' ').filter(x => x);
    let ds = [];
    for (let i = 0; i < u.length; i += 2) {
        const label = u[i];
        const szIdx = s.indexOf(label);
        const sz = szIdx !== -1 ? parseFloat(s[szIdx + 1]).toFixed(0) : "?";
        ds.push(`<div style="font-size:0.75rem;">${label} ${u[i + 1]} (${sz}GB)</div>`);
    }
    return ds.join('');
}

// --- 2. ADMIN YÖNETİM ---

async function loadAdminData() {
    loadRequests();
    loadUsers();
    loadManagerTags();
}

async function loadRequests() {
    try {
        const reqs = await api.get("/api/Admin/requests");
        const tbody = document.getElementById("requestRows");
        if (reqs.length === 0) { tbody.innerHTML = '<tr><td colspan="3" class="text-center muted-text">Bekleyen talep yok.</td></tr>'; return; }
        tbody.innerHTML = reqs.map(r => `
            <tr><td>${r.username}</td><td>${r.requestedRoleName || 'Bilinmiyor'}</td>
            <td><button class="btn primary small" onclick="approveUser(${r.id})">Onayla</button></td></tr>`).join("");
    } catch (e) { }
}

async function loadUsers() {
    try {
        const users = await api.get("/api/Admin/users");
        document.getElementById("userRows").innerHTML = users.map(u => `
            <tr><td><strong>${u.username}</strong></td>
                <td><select id="role_${u.id}" class="small-select">
                    <option value="1" ${u.roles.includes('Yönetici') ? 'selected' : ''}>Yönetici</option>
                    <option value="2" ${u.roles.includes('Denetleyici') ? 'selected' : ''}>Denetleyici</option>
                    <option value="3" ${u.roles.includes('Görüntüleyici') ? 'selected' : ''}>Görüntüleyici</option>
                </select></td>
                <td><div style="display:flex; gap:5px;">
                    <button class="btn primary small" onclick="changeUserRole(${u.id})" title="Kaydet">💾</button>
                    <button class="btn danger small" onclick="deleteUser(${u.id}, '${u.username}')" title="Sil">🗑️</button>
                </div></td></tr>`).join("");
    } catch (e) { }
}

window.deleteUser = async (id, name) => { if (confirm(`"${name}" tamamen silinecek. Emin misin?`)) { await api.del(`/api/Admin/users/${id}`); loadUsers(); } };

async function loadManagerTags() {
    try {
        const tags = await api.get("/api/Users/tags");
        document.getElementById("tagManagerList").innerHTML = tags.map(t => `
            <div class="pill" style="display:flex; align-items:center; gap:8px;">${t.name} <span onclick="deleteTag(${t.id})" style="cursor:pointer; color:#ef4444; font-weight:bold;">×</span></div>`).join("");
    } catch (e) { }
}

window.createNewTag = async () => {
    const n = document.getElementById("newTagName").value.trim();
    if (n) {
        try {
            await api.post("/api/Admin/tags", { name: n });
            document.getElementById("newTagName").value = "";
            loadManagerTags();
            loadFilterTags();
        } catch (e) { alert(e.message); }
    }
};

window.deleteTag = async (id) => { if (confirm("Etiket silinsin mi?")) { try { await api.del(`/api/Admin/tags/${id}`); loadManagerTags(); loadFilterTags(); } catch (e) { alert(e.message); } } };

window.approveUser = async (id) => { try { await api.post(`/api/Registration/approve/${id}`); loadAdminData(); } catch (e) { } };

window.changeUserRole = async (id) => {
    const rId = document.getElementById(`role_${id}`).value;
    try { await api.put(`/api/Admin/users/${id}/change-role`, { newRoleId: parseInt(rId) }); alert("Rol güncellendi."); } catch (e) { alert(e.message); }
};

window.handleRename = async (id, current) => {
    const n = prompt(`"${current}" için yeni takma ad:`, current);
    if (n && n.trim() !== "") { try { await api.put("/api/Admin/update-display-name", { id: id, newDisplayName: n.trim() }); loadAgents(); } catch (e) { alert(e.message); } }
};

// --- 3. AYARLAR MODAL (EŞİKLER VE ETİKET ATAMA) ---

window.openThresholdSettings = async (computerId) => {
    document.getElementById('modalComputerId').value = computerId;

    try {
        // Cihaz detaylarını ve diskleri çek
        const disks = await api.get(`/api/Admin/computers/${computerId}/disks`);
        const details = await api.get(`/api/Admin/computers/${computerId}`);

        // Eşikleri doldur
        document.getElementById('cpuThresholdInput').value = details.cpuThreshold || "";
        document.getElementById('ramThresholdInput').value = details.ramThreshold || "";

        // Diskleri Geniş Alan Olarak Doldur
        const container = document.getElementById('diskThresholdsContainer');
        container.innerHTML = '';
        disks.forEach(d => {
            container.innerHTML += `
            <div style="display:flex; align-items:center; justify-content:space-between; margin-bottom:12px; padding:10px; background:rgba(255,255,255,0.02); border-radius:6px; border:1px solid rgba(255,255,255,0.05);">
                <span style="font-weight:bold; font-size:0.95rem;">${d.diskName} Sürücüsü (${d.totalSizeGb.toFixed(0)} GB)</span>
                <div style="display:flex; align-items:center; gap:5px;">
                    <input type="number" class="disk-threshold-input" data-name="${d.diskName}" value="${d.thresholdPercent || ""}" style="width:75px; text-align:right; padding:5px; border-radius:4px;" min="0" max="100">
                    <span style="color:#94a3b8; font-weight:bold;">%</span>
                </div>
            </div>`;
        });

        // BİLGİSAYARA ETİKET ATAMA KISMI
        const allTags = await api.get("/api/Users/tags");
        const $mTags = $('#modalTagSelect');
        $mTags.empty();
        allTags.forEach(t => {
            const isSelected = details.tags && details.tags.includes(t.name);
            $mTags.append(new Option(t.name, t.name, isSelected, isSelected));
        });

        // Select2'yi modal içinde başlat (dropdownParent modal olmalı yoksa arkada kalır)
        $mTags.select2({
            dropdownParent: $('#thresholdModal'),
            width: '100%',
            placeholder: "Etiket seçin..."
        }).trigger('change');

        document.getElementById('thresholdModal').style.display = 'flex';
    } catch (e) { alert("Cihaz bilgileri yüklenemedi: " + e.message); }
};

window.saveThresholds = async () => {
    const id = document.getElementById('modalComputerId').value;
    const cpu = document.getElementById('cpuThresholdInput').value;
    const ram = document.getElementById('ramThresholdInput').value;
    const tags = $('#modalTagSelect').val() || [];

    // 0-100 Validation
    const validate = (v) => { if (v === "" || v === null) return true; const n = parseFloat(v); return n >= 0 && n <= 100; };
    if (!validate(cpu) || !validate(ram)) { alert("Hata: Değerler 0-100 arasında olmalıdır!"); return; }

    const disks = [];
    let dErr = false;
    document.querySelectorAll('.disk-threshold-input').forEach(i => {
        if (!validate(i.value)) dErr = true;
        disks.push({ diskName: i.getAttribute('data-name'), thresholdPercent: i.value === "" ? null : parseFloat(i.value) });
    });
    if (dErr) { alert("Hata: Disk değerleri 0-100 arasında olmalıdır!"); return; }

    try {
        // 1. Eşikleri Kaydet
        await api.put(`/api/Admin/update-thresholds/${id}`, {
            cpuThreshold: cpu === "" ? null : parseFloat(cpu),
            ramThreshold: ram === "" ? null : parseFloat(ram),
            diskThresholds: disks
        });

        // 2. Bilgisayara Etiketleri Kaydet
        await api.put(`/api/Admin/computers/${id}/tags`, { tags: tags });

        alert("✅ Ayarlar ve etiketler başarıyla kaydedildi.");
        closeModal();
        loadAgents();
    } catch (e) { alert("Kaydetme hatası: " + e.message); }
};

// --- INIT ---
loadFilterTags();
loadAgents();
setInterval(loadAgents, 10000);