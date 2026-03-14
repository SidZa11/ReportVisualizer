$(document).ready(function () {
    var tableConfigModal = $('#tableConfigModal');
    var tableSelect = $('#tableSelect');
    var columnCheckboxes = $('#columnCheckboxes');
    var runQueryButton = $('#runQueryButton');

    // Function to load tables
    function loadTables() {
        $.ajax({
            url: '?handler=Tables',
            type: 'GET',
            success: function (data) {
                tableSelect.empty();
                tableSelect.append('<option value="">-- Select a Table --</option>');
                if (data && data.length > 0) {
                    $.each(data, function (i, table) {
                        tableSelect.append('<option value="' + table + '">' + table + '</option>');
                    });
                } else {
                    tableSelect.append('<option value="">No tables found</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error("Error loading tables:", error);
                alert("Error loading tables: " + xhr.responseText);
            }
        });
    }

    // Function to load columns for a selected table
    function loadColumns(tableName) {
        columnCheckboxes.empty();
        if (tableName) {
            $.ajax({
                url: '?handler=Columns&tableName=' + tableName,
                type: 'GET',
                success: function (data) {
                    if (data && data.length > 0) {
                        $.each(data, function (i, column) {
                            columnCheckboxes.append(
                                '<div class="form-check">' +
                                '<input class="form-check-input" type="checkbox" value="' + column + '" id="column-' + column + '">' +
                                '<label class="form-check-label" for="column-' + column + '">' + column + '</label>' +
                                '</div>'
                            );
                        });
                    } else {
                        columnCheckboxes.append('<p>No columns found for this table.</p>');
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error loading columns:", error);
                    alert("Error loading columns: " + xhr.responseText);
                }
            });
        } else {
            columnCheckboxes.append('<p>Select a table to load columns.</p>');
        }
    }

    // Event listener for modal show
    tableConfigModal.on('show.bs.modal', function () {
        loadTables();
        columnCheckboxes.empty();
        columnCheckboxes.append('<p>Select a table to load columns.</p>');
    });

    // Event listener for table selection change
    tableSelect.on('change', function () {
        var selectedTable = $(this).val();
        loadColumns(selectedTable);
    });

    // Event listener for Run Query button click
    runQueryButton.on('click', function () {
        var selectedTable = tableSelect.val();
        var selectedColumns = [];
        columnCheckboxes.find('input[type="checkbox"]:checked').each(function () {
            selectedColumns.push($(this).val());
        });

        if (!selectedTable) {
            alert("Please select a table.");
            return;
        }

        if (selectedColumns.length === 0) {
            alert("Please select at least one column.");
            return;
        }

        // Send data to backend
        $.ajax({
            url: '?handler=ExecuteQuery',
            type: 'POST',
            headers: {
                RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            contentType: 'application/json',
            data: JSON.stringify({
                tableName: selectedTable,
                columns: selectedColumns
            }),
            success: function (response) {
                if (response.success) {
                    alert("Query execution initiated: " + response.message);
                    tableConfigModal.modal('hide');
                } else {
                    alert("Error: " + response.error);
                }
            },
            error: function (xhr, status, error) {
                console.error("Error executing query:", error);
                alert("Error executing query: " + xhr.responseText);
            }
        });
    });
});