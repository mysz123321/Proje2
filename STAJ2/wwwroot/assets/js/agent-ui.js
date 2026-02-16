// 1. API İsteği Fonksiyonu (İsim Güncelleme)
async function updateComputerDisplayName(id, newName) {
    const response = await fetch('/api/Admin/update-display-name', {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('token')}` // Admin yetkisi için
        },
        body: JSON.stringify({
            id: id,
            newDisplayName: newName
        })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Güncelleme başarısız oldu.');
    }

    return await response.json();
}

// 2. Butona Tıklandığında Çalışacak İşlem
async function handleRename(id, currentName) {
    const newName = prompt(`"${currentName}" cihazı için yeni bir takma ad giriniz:`, currentName);

    if (newName === null) return; // İptal edildi
    if (newName.trim() === "") {
        alert("İsim boş olamaz!");
        return;
    }

    try {
        await updateComputerDisplayName(id, newName.trim());
        alert("Cihaz ismi başarıyla güncellendi.");
        loadAgents(); // Tabloyu yenile
    } catch (err) {
        alert("Hata: " + err.message);
    }
}

// 3. Tabloyu Yükleme Fonksiyonu
async function loadAgents() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);

        const agents = await res.json();

        if (!Array.isArray(agents) || agents.length === 0) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center">Kayıtlı cihaz bulunamadı</td></tr>`;
            return;
        }

        tbody.innerHTML = agents.map(a => {
            // Backend'den gelen veriye göre (DiskUsage string formatındaydı)
            const diskInfo = a.diskUsage || "-";
            const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";

            // DisplayName varsa onu göster, yoksa MachineName kullan
            const currentDisplayName = a.displayName || a.machineName || "-";

            return `
                <tr>
                    <td>${a.macAddress ?? "-"}</td>
                    <td>
                        <strong>${currentDisplayName}</strong>
                        <br/><small class="text-muted">${a.machineName ?? "-"}</small>
                    </td>
                    <td>${a.ip ?? "-"}</td>
                    <td>%${a.cpuUsage ?? "0"}</td>
                    <td>%${a.ramUsage ?? "0"}</td>
                    <td><small>${diskInfo}</small></td>
                    <td>${ts}</td>
                    <td>
                        <button class="btn btn-sm btn-primary" onclick="handleRename(${a.computerId}, '${currentDisplayName}')">
                            ✏️ İsim Değiştir
                        </button>
                    </td>
                </tr>
            `;
        }).join("");
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="8" class="text-danger">Hata: ${e.message}</td></tr>`;
    }
}

// İlk yükleme ve periyodik yenileme
loadAgents();
setInterval(loadAgents, 5000);