
var datatable;
$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    datatable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getAll' },
        "columns": [
            { data: 'orderHeaderId', "width": "5%" },
            { data: 'name', "width": "20%" },
            { data: 'phoneNumber', "width": "18%" },
            { data: 'applicationUser.email', "width": "15%" },
            { data: 'orderStatus', "width": "10%" },
            { data: 'orderTotal', "width": "10%" },
            {
                data: 'OrderHeaderId',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                           <a href="/admin/order/details?id=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i>
                                </a>
                               
                    </div>`
                },
                "width": "10%"
            }
        ]
    });
}
