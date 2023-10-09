const getLiveTime = () => {
    const nowStr = new Date().toISOString();
    return [nowStr.slice(11, 13), nowStr.slice(14, 16), nowStr.slice(17, 19)];
}

const updateTimeDisplay = (hoursElement, minutesElement, secondsElement) =>
{
    const [hours, minutes, seconds] = getLiveTime();
    if (hoursElement.textContent !== hours) { hoursElement.textContent = hours }
    if (minutesElement.textContent !== minutes) { minutesElement.textContent = minutes }
    if (secondsElement.textContent !== seconds) { secondsElement.textContent = seconds }
}

export function startTimeUpdate (hoursElement, minutesElement, secondsElement) {
    updateTimeDisplay(hoursElement, minutesElement, secondsElement);
    setInterval(updateTimeDisplay, 200, hoursElement, minutesElement, secondsElement);
}

export function reveal (element) {
    element.classList.remove("invisible");
}