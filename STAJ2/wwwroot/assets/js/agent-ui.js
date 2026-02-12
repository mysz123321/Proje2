async function loadAgents() {
    const tbody = document.getElementById("agentRows");
    if (!tbody) return;

    try {
        const res = await fetch("/api/agent-telemetry/latest", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);

        const agents = await res.json();

        if (!Array.isArray(agents) || agents.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7">Kayıt yok</td></tr>`;
            return;
        }

        tbody.innerHTML = agents.map(a => {
            const disks = (a.disks || [])
                .map(d => {
                    const freeGb = (d.freeBytes / (1024 ** 3)).toFixed(1);
                    const totalGb = (d.totalBytes / (1024 ** 3)).toFixed(1);
                    return `${d.name} ${freeGb} / ${totalGb} GB`;
                })
                .join("<br/>");

            const ts = a.ts ? new Date(a.ts).toLocaleString() : "-";

            return `
        <tr>
          <td>${a.agentId ?? "-"}</td>
          <td>${a.machineName ?? "-"}</td>
          <td>${a.ip ?? "-"}</td>
          <td>${a.cpuPercent ?? "-"}</td>
          <td>${a.availableRamMb ?? "-"}</td>
          <td>${disks || "-"}</td>
          <td>${ts}</td>
        </tr>
      `;
        }).join("");
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="7">Hata: ${e.message}</td></tr>`;
    }
}

// ilk yükleme + 5 sn’de bir yenile
loadAgents();
setInterval(loadAgents, 5000);
