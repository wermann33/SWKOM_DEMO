const apiUrl = 'http://localhost:8080/todo';

// Function to fetch and display Todo items
function fetchTodoItems() {
    console.log('Fetching Todo items...');
    fetch(apiUrl)
        .then(response => response.json())
        .then(data => {
            const todoList = document.getElementById('todoList');
            todoList.innerHTML = ''; // Clear the list before appending new items
            data.forEach(task => {
                // Create list item with delete and toggle complete buttons
                const li = document.createElement('li');
                li.innerHTML = `
                    <span>Task: ${task.name} | Completed: ${task.isComplete}</span>
                    <button class="delete" style="margin-left: 10px;" onclick="deleteTask(${task.id})">Delete</button>
                    <button style="margin-left: 10px;" onclick="toggleComplete(${task.id}, ${task.isComplete}, '${task.name}')">
                        Mark as ${task.isComplete ? 'Incomplete' : 'Complete'}
                    </button>
                    <br/>
                    <br/>
                    <span>File: ${task.fileName || "No file uploaded"}</span>
                    <input type="file" id="fileInput${task.id}" />
                    <button style="margin-left: 10px;" onclick="uploadFile(${task.id}, document.getElementById('fileInput${task.id}'))">
                        Upload File
                    </button>
                    <br/>
                    <br/>
                    <br/>
                    <br/>
                `;
                todoList.appendChild(li);
            });
        })
        .catch(error => console.error('Fehler beim Abrufen der Todo-Items:', error));
}


// Function to add a new task
function addTask() {
    const taskName = document.getElementById('taskName').value;
    const isComplete = document.getElementById('isComplete').checked;
    const errorDiv = document.getElementById('errorMessages'); 


    const newTask = {
        name: taskName,
        isComplete: isComplete
    };

    fetch(apiUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(newTask)
    })
        .then(response => {
            if (response.ok) {
                fetchTodoItems(); // Refresh the list after adding
                document.getElementById('taskName').value = ''; // Clear the input field
                document.getElementById('isComplete').checked = false; // Reset checkbox
                errorDiv.innerHTML = ''; 
            } else {
                response.json().then(err => {
                    errorDiv.innerHTML = `<ul>` + Object.values(err.errors).map(e => `<li>${e}</li>`).join('') + `</ul>`;
                });
            }
        })
        .catch (error => console.error('Fehler:', error));
}

function uploadFile(taskId, fileInput) {
    const file = fileInput.files[0];
    const errorDiv = document.getElementById('errorMessages'); // Div für Fehlernachrichten

    if (!file) {
        errorDiv.innerHTML = `<ul><li>Keine Datei ausgewählt.</li></ul>`;
        return;
    }

    const formData = new FormData();
    formData.append('taskFile', file);

    fetch(`${apiUrl}/${taskId}/upload`, {
        method: 'PUT',
        body: formData
    })
        .then(response => {
            if (response.ok) {
                fetchTodoItems();
                errorDiv.innerHTML = ''; // Fehlernachrichten bei Erfolg entfernen
            } else {
                // Versuche die Fehlernachricht zu lesen, falls vorhanden
                response.json().then(err => {
                    if (err.errors) {
                        errorDiv.innerHTML = `<ul>` + Object.values(err.errors).map(e => `<li>${e}</li>`).join('') + `</ul>`;
                    } else {
                        // Allgemeine Fehlermeldung, falls keine spezifische Nachricht vorhanden ist
                        errorDiv.innerHTML = `<ul><li>Fehler beim Hochladen der Datei. Nur PDF-Dateien sind erlaubt.</li></ul>`;
                    }
                }).catch(() => {
                    errorDiv.innerHTML = `<ul><li>Fehler beim Hochladen der Datei.</li></ul>`;
                });
            }
        })
        .catch(error => {
            console.error('Fehler:', error);
            errorDiv.innerHTML = `<ul><li>Fehler beim Verbinden mit dem Server.</li></ul>`;
        });
}



// Function to delete a task
function deleteTask(id) {
    fetch(`${apiUrl}/${id}`, {
        method: 'DELETE'
    })
        .then(response => {
            if (response.ok) {
                fetchTodoItems(); // Refresh the list after deletion
            } else {
                console.error('Fehler beim Löschen der Aufgabe.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}

// Function to toggle complete status
function toggleComplete(id, isComplete, name) {
    // Aufgabe mit umgekehrtem isComplete-Status aktualisieren
    const updatedTask = {
        id: id,  // Die ID des Tasks
        name: name, // Der Name des Tasks
        isComplete: !isComplete // Status umkehren
    };

    fetch(`${apiUrl}/${id}`, {
        method: 'PUT',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(updatedTask)
    })
        .then(response => {
            if (response.ok) {
                fetchTodoItems(); // Liste nach dem Update aktualisieren
                console.log('Erfolgreich aktualisiert.');
            } else {
                console.error('Fehler beim Aktualisieren der Aufgabe.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}


// Load todo items on page load
document.addEventListener('DOMContentLoaded', fetchTodoItems);
