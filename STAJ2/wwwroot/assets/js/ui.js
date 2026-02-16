// Kullanıcıları listelerken rol sütununa bunu ekle:
function renderUserRow(user) {
    return `
        <tr>
            <td>${user.username}</td>
            <td>
                <select id="roleSelect_${user.id}" class="role-select">
                    <option value="1" ${user.role === 'Yönetici' ? 'selected' : ''}>Yönetici</option>
                    <option value="2" ${user.role === 'İzleyici' ? 'selected' : ''}>İzleyici</option>
                    <option value="3" ${user.role === 'Denetçi' ? 'selected' : ''}>Denetçi</option>
                </select>
            </td>
            <td>
                <button onclick="changeRole(${user.id})" class="btn-primary">Rolü Güncelle</button>
            </td>
        </tr>
    `;
}

async function changeRole(userId) {
    const newRoleId = document.getElementById(`roleSelect_${userId}`).value;

    const response = await fetch(`/api/Admin/users/${userId}/change-role`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ newRoleId: parseInt(newRoleId) })
    });

    if (response.ok) {
        alert("Kullanıcı rolü güncellendi!");
    }
}