document.addEventListener('DOMContentLoaded', function() {
    var usernameField = document.getElementById('username');
    var passwordField = document.getElementById('password');
    var emailField = document.getElementById('email');
    var fullnameField = document.getElementById('fullname');
    var usernameError = document.getElementById('username-error');
    var passwordError = document.getElementById('password-error');
    var emailError = document.getElementById('email-error');
    var fullnameError = document.getElementById('fullname-error');

    function isValidEmail(email) {
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    function hasUpperCase(password) {
        // Check if password contains at least one uppercase letter
        return /[A-Z]/.test(password);
    }

    function hasDigit(password) {
        // Check if password contains at least one digit
        return /\d/.test(password);
    }

    usernameField.addEventListener('blur', function() {
        if (usernameField.value.trim() === '') {
            usernameError.textContent = 'Username Required';
            usernameError.style.display = 'inline';
        } else {
            usernameError.style.display = 'none';
        }
    });

    passwordField.addEventListener('blur', function() {
        var password = passwordField.value.trim();
        if (password === '') {
            passwordError.textContent = 'Password Required';
            passwordError.style.display = 'inline';
        } else if (password.length < 8) {
            passwordError.textContent = 'Password must be at least 8 characters long';
            passwordError.style.display = 'inline';
        } else if (!hasUpperCase(password)) {
            passwordError.textContent = 'Password must contain at least one uppercase letter';
            passwordError.style.display = 'inline';
        } else if (!hasDigit(password)) {
            passwordError.textContent = 'Password must contain at least one digit';
            passwordError.style.display = 'inline';
        } else {
            passwordError.style.display = 'none';
        }
    });

    emailField.addEventListener('blur', function() {
        var email = emailField.value.trim();
        if (email === '') {
            emailError.textContent = 'Email Required';
            emailError.style.display = 'inline';
        } else if (!isValidEmail(email)) {
            emailError.textContent = 'Invalid Email Format';
            emailError.style.display = 'inline';
        } else {
            emailError.style.display = 'none';
        }
    });

    fullnameField.addEventListener('blur', function() {
        if (fullnameField.value.trim() === '') {
            fullnameError.textContent = 'Fullname Required';
            fullnameError.style.display = 'inline';
        } else {
            fullnameError.style.display = 'none';
        }
    });

    // Toggle password visibility
    var togglePassword = document.getElementById('togglePassword');
    togglePassword.addEventListener('click', function() {
        var passwordInput = document.getElementById('password');
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            togglePassword.classList.remove('fa-eye');
            togglePassword.classList.add('fa-eye-slash'); // Change to eye-slash icon if using Font Awesome
        } else {
            passwordInput.type = 'password';
            togglePassword.classList.remove('fa-eye-slash');
            togglePassword.classList.add('fa-eye'); // Change to eye icon if using Font Awesome
        }
    });
});