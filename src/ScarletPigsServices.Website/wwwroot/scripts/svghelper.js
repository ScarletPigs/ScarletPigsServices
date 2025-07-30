function elementExists(elementId) {
    return document.getElementById(elementId) !== null;
}

function clearLines(svgContainerId) {
    const svgContainer = document.getElementById(svgContainerId);
    const svg = svgContainer.querySelector('svg');
    svg.innerHTML = '';
}

function drawRightAngledLine(svgContainerId, startElementId, endElementId, color, thickness) {
    const svgContainer = document.getElementById(svgContainerId);
    const svg = svgContainer.querySelector('svg');

    if (startElementId && endElementId && svgContainerId) {
        const startElement = document.getElementById(startElementId);
        const endElement = document.getElementById(endElementId);
        const svgRect = svgContainer.getBoundingClientRect();
        const startRect = startElement.getBoundingClientRect();
        const endRect = endElement.getBoundingClientRect();

        const startX = startRect.left - svgRect.left + (startRect.width / 2);
        const startY = startRect.top - svgRect.top + (startRect.height / 2);
        const endX = endRect.left - svgRect.left + (endRect.width / 2);
        const endY = endRect.top - svgRect.top + (endRect.height / 2);

        // Before drawing, check if a similar path already exists
        const existingPaths = svg.querySelectorAll('path');
        const tolerance = 0.5; // Adjust as needed for floating-point precision
        for (let i = 0; i < existingPaths.length; i++) {
            const existingPath = existingPaths[i];
            const d = existingPath.getAttribute('d');
            const parsed = parseDAttribute(d);
            if (parsed) {
                const sameStart = Math.abs(parsed.startX - startX) < tolerance && Math.abs(parsed.startY - startY) < tolerance;
                const sameEnd = Math.abs(parsed.endX - endX) < tolerance && Math.abs(parsed.endY - endY) < tolerance;
                if (sameStart && sameEnd) {
                    // Similar path already exists, return
                    return;
                }
            }
        }

        // Modified path to continue vertically until the same height as the destination
        const dPath = `M ${startX} ${startY} V ${endY} H ${endX}`;

        const newPath = document.createElementNS("http://www.w3.org/2000/svg", "path");
        newPath.setAttribute("d", dPath);
        newPath.setAttribute("stroke", color);
        newPath.setAttribute("stroke-width", thickness);
        newPath.setAttribute("fill", "none");

        svg.appendChild(newPath);
    }

    function parseDAttribute(d) {
        // Expected format: "M startX startY V endY H endX"
        const regex = /M\s+([-+]?[0-9]*\.?[0-9]+)\s+([-+]?[0-9]*\.?[0-9]+)\s+V\s+([-+]?[0-9]*\.?[0-9]+)\s+H\s+([-+]?[0-9]*\.?[0-9]+)/;
        const match = d.match(regex);
        if (match) {
            const startX = parseFloat(match[1]);
            const startY = parseFloat(match[2]);
            const endY = parseFloat(match[3]);
            const endX = parseFloat(match[4]);
            return { startX, startY, endX, endY };
        } else {
            return null;
        }
    }
}
