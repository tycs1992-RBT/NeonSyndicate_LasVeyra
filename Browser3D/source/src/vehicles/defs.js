// Vehicle roster (all original names). Dimensions in meters, speeds in m/s.
// Body stations: [z from rear, bottom, belt (top of lower body), roof (top of greenhouse or belt), halfWidthLower, halfWidthTop]
export const VEHICLES = {
  arrow: {
    name: 'Vanta Arrow', cls: 'sports', length: 4.5, width: 1.92, height: 1.24, wheelbase: 2.65, wheelR: 0.34, track: 0.82,
    mass: 1350, top: 62, accel: 9.5, brake: 16, grip: 9.0, steer: 0.62, price: 90000,
    colors: ['#c9141c', '#f2f2f2', '#111317', '#1f4fbf', '#e8b31a', '#7a8088'],
    stations: [[0, 0.32, 0.66, 0.66, 0.84, 0.84], [0.2, 0.24, 0.8, 0.8, 0.94, 0.94], [0.75, 0.2, 0.84, 0.86, 0.96, 0.9], [1.25, 0.2, 0.86, 1.18, 0.95, 0.66], [2.45, 0.2, 0.86, 1.22, 0.95, 0.68], [3.15, 0.2, 0.8, 0.84, 0.95, 0.82], [4.15, 0.24, 0.66, 0.66, 0.93, 0.93], [4.5, 0.3, 0.52, 0.52, 0.86, 0.86]],
    roofStart: 1.25, roofEnd: 2.45, cowl: 3.15, rearDeck: 0.75,
  },
  vektor: {
    name: 'Bishop Vektor', cls: 'super', length: 4.6, width: 2.0, height: 1.12, wheelbase: 2.7, wheelR: 0.35, track: 0.86,
    mass: 1400, top: 72, accel: 12, brake: 18, grip: 10.5, steer: 0.58, price: 240000,
    colors: ['#ff8a00', '#d8d8d8', '#0b0b0d', '#8ad01a', '#c0103a'],
    stations: [[0, 0.3, 0.7, 0.7, 0.9, 0.9], [0.3, 0.2, 0.82, 0.84, 0.98, 0.98], [1.1, 0.18, 0.84, 1.06, 0.99, 0.62], [2.2, 0.18, 0.82, 1.1, 0.99, 0.64], [3.0, 0.18, 0.7, 0.74, 0.98, 0.8], [4.2, 0.22, 0.52, 0.52, 0.94, 0.94], [4.6, 0.26, 0.42, 0.42, 0.84, 0.84]],
    roofStart: 1.1, roofEnd: 2.2, cowl: 3.0, rearDeck: 0.3,
  },
  falcon: {
    name: 'Torque Falcon', cls: 'muscle', length: 4.95, width: 1.95, height: 1.32, wheelbase: 2.9, wheelR: 0.36, track: 0.84,
    mass: 1600, top: 60, accel: 10.5, brake: 13, grip: 7.0, steer: 0.6, price: 75000,
    colors: ['#141518', '#8a1010', '#1b3d7a', '#d9d4c8', '#2f5a2f', '#e0a020'],
    stations: [[0, 0.3, 0.74, 0.74, 0.86, 0.86], [0.2, 0.24, 0.86, 0.86, 0.95, 0.95], [0.9, 0.22, 0.88, 0.9, 0.97, 0.94], [1.35, 0.22, 0.88, 1.26, 0.97, 0.72], [2.55, 0.22, 0.88, 1.3, 0.97, 0.72], [3.1, 0.22, 0.88, 0.9, 0.97, 0.88], [4.6, 0.26, 0.8, 0.8, 0.96, 0.96], [4.95, 0.3, 0.64, 0.64, 0.9, 0.9]],
    roofStart: 1.35, roofEnd: 2.55, cowl: 3.1, rearDeck: 0.9,
  },
  pebble: {
    name: 'Moto Pebble', cls: 'compact', length: 3.9, width: 1.72, height: 1.48, wheelbase: 2.45, wheelR: 0.31, track: 0.74,
    mass: 1050, top: 44, accel: 6.5, brake: 13, grip: 7.5, steer: 0.68, price: 14000,
    colors: ['#d9d4c8', '#5a8fb8', '#b83a2a', '#3a3a3a', '#8fb86a', '#e8c23a'],
    stations: [[0, 0.3, 0.86, 0.86, 0.82, 0.82], [0.12, 0.24, 0.92, 1.0, 0.86, 0.84], [0.35, 0.22, 0.94, 1.42, 0.87, 0.72], [2.25, 0.22, 0.94, 1.46, 0.87, 0.72], [2.85, 0.22, 0.92, 0.96, 0.87, 0.82], [3.6, 0.24, 0.78, 0.78, 0.86, 0.86], [3.9, 0.3, 0.62, 0.62, 0.8, 0.8]],
    roofStart: 0.35, roofEnd: 2.25, cowl: 2.85, rearDeck: 0.12,
  },
  kestrel: {
    name: 'Kestrel Sedan', cls: 'sedan', length: 4.8, width: 1.84, height: 1.45, wheelbase: 2.8, wheelR: 0.33, track: 0.78,
    mass: 1400, top: 50, accel: 7.2, brake: 14, grip: 7.8, steer: 0.62, price: 22000,
    colors: ['#d9d9d9', '#2a2c30', '#6a7280', '#7a1c1c', '#1f3a5a', '#c8b89a', '#f2f2f2'],
    stations: [[0, 0.3, 0.74, 0.74, 0.84, 0.84], [0.18, 0.22, 0.9, 0.9, 0.9, 0.9], [0.95, 0.2, 0.92, 0.95, 0.92, 0.88], [1.45, 0.2, 0.93, 1.4, 0.92, 0.7], [2.95, 0.2, 0.94, 1.44, 0.92, 0.7], [3.55, 0.2, 0.92, 0.95, 0.92, 0.84], [4.45, 0.24, 0.8, 0.8, 0.9, 0.9], [4.8, 0.3, 0.62, 0.62, 0.84, 0.84]],
    roofStart: 1.45, roofEnd: 2.95, cowl: 3.55, rearDeck: 0.95,
  },
  rover: {
    name: 'Dune Rover', cls: 'suv', length: 4.85, width: 1.98, height: 1.82, wheelbase: 2.85, wheelR: 0.4, track: 0.84,
    mass: 2100, top: 46, accel: 6.5, brake: 12, grip: 6.8, steer: 0.6, price: 38000,
    colors: ['#1a1c20', '#e8e8e8', '#3a4a3a', '#7a6a50', '#2a3a5a', '#8a1a1a'],
    stations: [[0, 0.42, 1.08, 1.08, 0.94, 0.94], [0.15, 0.36, 1.12, 1.66, 0.97, 0.86], [3.0, 0.36, 1.12, 1.74, 0.98, 0.86], [3.6, 0.36, 1.1, 1.16, 0.98, 0.92], [4.55, 0.4, 1.02, 1.02, 0.97, 0.97], [4.85, 0.44, 0.86, 0.86, 0.92, 0.92]],
    roofStart: 0.15, roofEnd: 3.0, cowl: 3.6, rearDeck: 0.15,
  },
  anvil: {
    name: 'Anvil Pickup', cls: 'pickup', length: 5.4, width: 2.0, height: 1.84, wheelbase: 3.3, wheelR: 0.41, track: 0.86,
    mass: 2300, top: 44, accel: 6.2, brake: 11, grip: 6.4, steer: 0.58, price: 30000,
    colors: ['#e8e8e8', '#8a1a1a', '#1a1c20', '#3a5a7a', '#6a6a5a'],
    stations: [[0, 0.44, 1.06, 1.06, 0.97, 0.97], [1.9, 0.42, 1.08, 1.08, 0.97, 0.97], [2.0, 0.42, 1.08, 1.72, 0.98, 0.86], [3.25, 0.42, 1.1, 1.78, 0.98, 0.86], [3.85, 0.42, 1.1, 1.14, 0.98, 0.94], [5.1, 0.44, 1.04, 1.04, 0.97, 0.97], [5.4, 0.48, 0.88, 0.88, 0.92, 0.92]],
    roofStart: 2.0, roofEnd: 3.25, cowl: 3.85, rearDeck: 1.9, bed: [0.15, 1.9],
  },
  courier: {
    name: 'Vanta Courier', cls: 'van', length: 5.2, width: 2.0, height: 2.15, wheelbase: 3.2, wheelR: 0.38, track: 0.86,
    mass: 2400, top: 40, accel: 5.5, brake: 11, grip: 6.2, steer: 0.6, price: 26000,
    colors: ['#f2f2f2', '#d9d9d9', '#2a3a5a', '#7a1a1a'],
    stations: [[0, 0.38, 2.05, 2.05, 0.98, 0.98], [3.9, 0.38, 2.1, 2.1, 0.99, 0.99], [4.45, 0.38, 1.12, 1.86, 0.99, 0.9], [5.0, 0.4, 1.02, 1.02, 0.98, 0.98], [5.2, 0.44, 0.86, 0.86, 0.92, 0.92]],
    roofStart: 3.9, roofEnd: 3.95, cowl: 4.45, rearDeck: 3.9, boxy: true,
  },
  roadster: {
    name: 'Coastline Roadster', cls: 'convertible', length: 4.3, width: 1.86, height: 1.15, wheelbase: 2.5, wheelR: 0.33, track: 0.8,
    mass: 1250, top: 60, accel: 9.0, brake: 15, grip: 8.8, steer: 0.64, price: 85000,
    colors: ['#f2f2f2', '#c9141c', '#1a1c20', '#4a7ab0', '#d8c08a'],
    stations: [[0, 0.3, 0.7, 0.7, 0.86, 0.86], [0.25, 0.22, 0.84, 0.84, 0.93, 0.93], [1.25, 0.2, 0.86, 0.86, 0.94, 0.9], [2.6, 0.2, 0.86, 1.08, 0.94, 0.86], [3.0, 0.2, 0.8, 0.82, 0.94, 0.88], [4.0, 0.24, 0.62, 0.62, 0.92, 0.92], [4.3, 0.3, 0.5, 0.5, 0.86, 0.86]],
    roofStart: 2.55, roofEnd: 2.6, cowl: 3.0, rearDeck: 1.25, open: true,
  },
  cab: {
    name: 'Vanta Metro Cab', cls: 'taxi', base: 'kestrel', fixedColor: '#f2c21a', taxi: true, top: 48, price: 0,
  },
  cruiser: {
    name: 'Metro Response Cruiser', cls: 'police', base: 'kestrel', fixedColor: '#141518', police: true, top: 58, accel: 9, grip: 8.6, mass: 1600, price: 0,
  },
  ambulance: {
    name: 'Saint Lumen Ambulance', cls: 'ambulance', base: 'courier', fixedColor: '#f4f4f4', ambulance: true, top: 44, price: 0,
  },
};

// Resolve "base" entries into full definitions.
for (const [key, d] of Object.entries(VEHICLES)) {
  d.key = key;
  if (d.base) {
    const b = VEHICLES[d.base];
    for (const [k, v] of Object.entries(b)) if (d[k] === undefined) d[k] = Array.isArray(v) ? v.slice() : v;
    d.colors = [d.fixedColor];
  }
}

export const TRAFFIC_MIX = [['kestrel', 26], ['pebble', 18], ['rover', 14], ['anvil', 10], ['courier', 8], ['arrow', 5], ['falcon', 6], ['roadster', 4], ['cab', 6], ['vektor', 1], ['cruiser', 3]];
