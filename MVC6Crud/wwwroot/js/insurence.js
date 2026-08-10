// Navbar scroll effect
window.addEventListener('scroll', function () {
    const navbar = document.querySelector('.navbar');
    if (window.scrollY > 50) {
        navbar.classList.add('navbar-scrolled');
    } else {
        navbar.classList.remove('navbar-scrolled');
    }
});

// Show loader
function showLoader() {
    $("#globalLoader").removeClass("d-none");
}

// Hide loader
function hideLoader() {
    $("#globalLoader").addClass("d-none");
}

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const targetId = this.getAttribute('href');
        if (targetId === '#') return;

        const targetElement = document.querySelector(targetId);
        if (targetElement) {
            window.scrollTo({
                top: targetElement.offsetTop - 70,
                behavior: 'smooth'
            });
        }
    });
});

// Animation on scroll
function animateOnScroll() {
    const elements = document.querySelectorAll('.feature-card, .category-card, .step-card');

    elements.forEach(element => {
        const elementPosition = element.getBoundingClientRect().top;
        const screenPosition = window.innerHeight / 1.2;

        if (elementPosition < screenPosition) {
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        }
    });
}

// Set initial styles for animation
document.querySelectorAll('.feature-card, .category-card, .step-card').forEach(element => {
    element.style.opacity = '0';
    element.style.transform = 'translateY(20px)';
    element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
});

// Run animation on load and scroll
window.addEventListener('load', animateOnScroll);
window.addEventListener('scroll', animateOnScroll);

// Form validation for payment page
if (document.getElementById('paymentForm')) {
    const paymentForm = document.getElementById('paymentForm');

    paymentForm.addEventListener('submit', function (e) {
        e.preventDefault();

        // Validate form fields
        const policyNumber = document.getElementById('policyNumber').value;
        const amount = document.getElementById('amount').value;

        if (!policyNumber || !amount) {
            alert('Please fill in all required fields');
            return;
        }

        // Submit form (simulate payment processing)
        const submitBtn = paymentForm.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;

        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
        submitBtn.disabled = true;

        // Simulate API call
        setTimeout(() => {
            alert('Payment processed successfully!');
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
            paymentForm.reset();
        }, 2000);
    });
}