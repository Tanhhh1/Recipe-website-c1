document.addEventListener('DOMContentLoaded', () => {
    function updateRemainingTime() {
        const currentTime = new Date();
        const contestElements = document.querySelectorAll('.list_ct');

        contestElements.forEach(contest => {
            const contestTime = contest.querySelector('.contest-time').textContent.trim();
            const [start, end] = contestTime.split(' - ');

            const startDate = new Date(start.split('/').reverse().join('-'));
            const endDate = new Date(end.split('/').reverse().join('-'));

            const remainingTime = endDate - currentTime;
            if (remainingTime > 0) {
                const days = Math.floor(remainingTime / (1000 * 60 * 60 * 24));
                const hours = Math.floor((remainingTime % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((remainingTime % (1000 * 60 * 60)) / (1000 * 60));

                contest.querySelector('.time_remaining').textContent = `${days}D : ${hours}H : ${minutes}M `;

                contest.querySelector('.btt_still').style.display = 'block';
                contest.querySelector('.btt_expired').style.display = 'none';
            } else {
                contest.querySelector('.btt_still').style.display = 'none';
                contest.querySelector('.btt_expired').style.display = 'block';
            }
        });
    }

    setInterval(updateRemainingTime, 1000);

    updateRemainingTime();
});