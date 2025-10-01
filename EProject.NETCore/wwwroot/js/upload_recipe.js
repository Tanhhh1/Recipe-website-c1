document.addEventListener('DOMContentLoaded', function () {
    const imagePreview = document.getElementById('img_import');
    const imagecnt = document.getElementById('img_content');

    if (imagePreview.src && imagePreview.src !== '') {
        imagePreview.style.display = 'block';
        imagecnt.style.display = 'none';
    }

    document.getElementById('import_form').addEventListener('change', function (event) {
        const file = event.target.files[0];

        if (file) {
            const fileName = file.name;
            const reader = new FileReader();
            reader.onload = function (e) {
                const namefilep = document.getElementById('name_file_p');
                imagePreview.src = e.target.result;
                imagePreview.style.display = 'block';
                imagecnt.style.display = 'none';
                namefilep.innerHTML = fileName;
            }
            reader.readAsDataURL(file);
        }
    });
});
const input = document.getElementById('recipe_name');
input.addEventListener('keydown', function (event) {
    if (event.key === 'Enter') {
        event.preventDefault();
    }
});
const textareas = document.querySelectorAll('.area_input');
textareas.forEach(textarea => {
    textarea.addEventListener('input', function () {
        const lineCount = (this.value.match(/\n/g) || []).length + 1;
        this.rows = lineCount;
    });
});