$(document).ready(function () {
    var tableConfigModal = $('#tableConfigModal');
    var tableSelect = $('#tableSelect');
    var columnCheckboxes = $('#columnCheckboxes');
    var runQueryButton = $('#runQueryButton');
    var dateTimeColumnSelect = $('#dateTimeColumnSelect');

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
        dateTimeColumnSelect.empty();
        dateTimeColumnSelect.append('<option value="">-- Select a Time Column --</option>');

        if (tableName) {
            $.ajax({
                url: '?handler=ColumnDataTypes&tableName=' + tableName,
                type: 'GET',
                success: function (data) {
                    if (data && data.length > 0) {
                        $.each(data, function (i, column) {
                            // Populate general column checkboxes
                            columnCheckboxes.append(
                                '<div class="form-check">' +
                                '<input class="form-check-input" type="checkbox" value="' + column.columnName + '" id="column-' + column.columnName + '">' +
                                '<label class="form-check-label" for="column-' + column.columnName + '">' + column.columnName + ' (' + column.dataType + ')</label>' +
                                '</div>'
                            );

                            // Populate time column dropdown with all columns
                            dateTimeColumnSelect.append('<option value="' + column.columnName + '">' + column.columnName + ' (' + column.dataType + ')</option>');
                        });
                    } else {
                        columnCheckboxes.append('<p>No columns found for this table.</p>');
                        dateTimeColumnSelect.append('<option value="">No time columns found</option>');
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error loading columns:", error);
                    alert("Error loading columns: " + xhr.responseText);
                }
            });
        } else {
            columnCheckboxes.append('<p>Select a table to load columns.</p>');
            dateTimeColumnSelect.append('<option value="">Select a table to load time columns.</option>');
        }
    }

    // Event listener for modal show
    tableConfigModal.on('show.bs.modal', function () {
        loadTables();
        columnCheckboxes.empty();
        columnCheckboxes.append('<p>Select a table to load columns.</p>');
        dateTimeColumnSelect.empty();
        dateTimeColumnSelect.append('<option value="">-- Select a Time Column --</option>');
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
        var selectedDateTimeColumn = dateTimeColumnSelect.val();

        if (!selectedTable) {
            alert("Please select a table.");
            return;
        }

        if (selectedColumns.length === 0) {
            alert("Please select at least one column for the query.");
            return;
        }

        if (!selectedDateTimeColumn) {
            alert("Please select a Time Column.");
            return;
        }

        var procedureName = prompt("Please enter the name for the stored procedure:", "usp_GenericTimeSeriesInterval");
        if (!procedureName) {
            alert("Stored procedure name cannot be empty.");
            return;
        }

        // Send data to backend
        sendExecuteQueryRequest(selectedTable, selectedColumns, selectedDateTimeColumn, procedureName);
    });

    function sendExecuteQueryRequest(selectedTable, selectedColumns, selectedDateTimeColumn, procedureName) {
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
                columns: selectedColumns,
                dateTimeColumn: selectedDateTimeColumn,
                procedureName: procedureName
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
    }
});