// STAJ2/wwwroot/assets/js/agent-ui.js

async function loadAgents() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);

        const agents = await res.json();

        if (!Array.isArray(agents) || agents.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center">Kayıtlı cihaz bulunamadı</td></tr>`;
            return;
        }

        tbody.innerHTML = agents.map(a => {
            const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";
            const deviceDisplayName = a.displayName || a.machineName || "-";
            const ramInfo = `%${a.ramUsage?.toFixed(1) ?? "0"} (${a.totalRamMb?.toFixed(0) ?? "0"} MB)`;

            let diskCombined = "-";
            if (a.diskUsage && a.totalDiskGb) {
                const usageParts = a.diskUsage.split(' ').filter(x => x.length > 0);
                const sizeParts = a.totalDiskGb.split(' ').filter(x => x.length > 0);

                let disks = [];
                for (let i = 0; i < usageParts.length; i += 2) {
                    const label = usageParts[i]; // "C:"
                    const usage = usageParts[i + 1]; // "%40.5"
                    const sizeIdx = sizeParts.indexOf(label);
                    const size = sizeIdx !== -1 ? sizeParts[sizeIdx + 1] : "?";

                    // BURASI DEĞİŞTİ: Her diski bir div içine alıyoruz ki alt alta düzgün dursun
                    disks.push(`<div style="margin-bottom: 2px;">${label} ${usage} (${parseFloat(size).toFixed(0)} GB)</div>`);
                }
                diskCombined = disks.join(''); // Artık div'ler kullandığımız için join boş kalabilir
            }

            return `
                <tr>
                    <td style="vertical-align: middle;"><strong>${deviceDisplayName}</strong></td>
                    <td style="vertical-align: middle;">${a.ip ?? "-"}</td>
                    <td style="vertical-align: middle;">%${a.cpuUsage?.toFixed(1) ?? "0"}</td>
                    <td style="vertical-align: middle;">${ramInfo}</td>
                    <td style="vertical-align: middle; padding-top: 8px; padding-bottom: 8px;">${diskCombined}</td>
                    <td style="vertical-align: middle;">${ts}</td>
                </tr>
            `;
        }).join("");
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-danger">Hata: ${e.message}</td></tr>`;
    }
}

// Periyodik yenileme
loadAgents();
setInterval(loadAgents, 10000);