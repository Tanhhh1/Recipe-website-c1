document.addEventListener("DOMContentLoaded", function() {
    const cards = document.querySelectorAll('.item_recipe');
    const loadMoreButton = document.querySelector('.load-more');
    let visibleCards = 8;

    if (visibleCards >= cards.length) {
        loadMoreButton.style.display = 'none';
    }

    for (let i = 0; i < visibleCards; i++) {
        if (cards[i]) {
            cards[i].style.display = 'block';
        }
    }

    loadMoreButton.addEventListener('click', function() {
        for (let i = visibleCards; i < visibleCards + 4; i++) {
            if (cards[i]) {
                cards[i].style.display = 'block';
            }
        }
        visibleCards += 4;

        if (visibleCards >= cards.length) {
            loadMoreButton.style.display = 'none';
        }
    });
    
});