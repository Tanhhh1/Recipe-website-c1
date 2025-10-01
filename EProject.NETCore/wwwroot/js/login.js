document.addEventListener('DOMContentLoaded', function() {
    var usernameField = document.getElementById('username');
    var passwordField = document.getElementById('password');
    var usernameError = document.getElementById('username-error');
    var passwordError = document.getElementById('password-error');

    usernameField.addEventListener('blur', function() {
        if (usernameField.value.trim() === '') {
            usernameError.textContent = 'Username Required';
            usernameError.style.display = 'inline';
        } else {
            usernameError.style.display = 'none';
        }
    });

    passwordField.addEventListener('blur', function() {
        if (passwordField.value.trim() === '') {
            passwordError.textContent = 'Password Required';
            passwordError.style.display = 'inline';
        } else {
            passwordError.style.display = 'none';
        }
    });
});
