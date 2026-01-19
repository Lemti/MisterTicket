import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type TicketType = 'Standard' | 'Accessible' | 'VIP';

export interface Seat {
  id: number;
  seatNumber: string;
  row: string;
  zone: string;
  price: number;
  status: string; // Available | Reserved | Paid
  courtId?: number;
}

interface Section {
  id: string; // stable key, ex: GH / GB / TG / TD / VIP...
  number: number; // display number
  label: string;  // displayed label
  ring: 'inner' | 'outer';
  type: TicketType;
  price: number;
  available: number;
  pathD: string;
  centroid: { x: number; y: number };
  seats: Seat[]; // ✅ real seats from backend
}

@Component({
  selector: 'app-seat-map',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './seat-map.component.html',
  styleUrls: ['./seat-map.component.css'],
})
export class SeatMapComponent implements OnInit, OnChanges {
  @Input() courtId: number = 0;
  @Input() eventId: number = 0;

  // ✅ seats coming from backend (admin generated)
  @Input() seats: Seat[] = [];

  @Input() courtName = 'Court';
  @Input() eventCategory = '';
  @Input() eventRound = '';

  // display only; will fallback to seats.length
  @Input() totalCapacity: number = 0;

  // UI
  ticketsCount = 1; // ✅ 1 ticket by default
  filterType: 'All' | TicketType = 'All';
  activeTab: 'Lowest' | 'Best' = 'Lowest';

  @Output() seatsSelected = new EventEmitter<Seat[]>();

  sections: Section[] = [];
  selectedSection: Section | null = null;
  selectedSeats: Seat[] = [];

  // SVG / zoom pan
  viewW = 1000;
  viewH = 700;
  panX = 0;
  panY = 0;
  scale = 1;

  private isDragging = false;
  private dragStart = { x: 0, y: 0 };
  private panStart = { x: 0, y: 0 };

  ngOnInit(): void {
    this.rebuildFromSeats();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['seats'] || changes['totalCapacity']) {
      this.rebuildFromSeats();
    }
  }

  private rebuildFromSeats() {
    const cap =
      this.totalCapacity && this.totalCapacity > 0
        ? this.totalCapacity
        : this.seats?.length ?? 0;

    this.totalCapacity = cap;

    this.sections = this.buildSectionsFromSeats(this.seats || []);
    this.selectedSection = null;
    this.selectedSeats = [];
  }

  // -------------------------
  // Build Ticketmaster-like sections from REAL seats
  // -------------------------
  private buildSectionsFromSeats(seats: Seat[]): Section[] {
    const groups = new Map<string, Seat[]>();

    for (const s of seats) {
      const key = this.getSectionKey(s);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(s);
    }

    const keys = Array.from(groups.keys()).sort((a, b) =>
      a.localeCompare(b)
    );

    const n = keys.length;

    // ring split
    const innerCount = n > 18 ? Math.ceil(n * 0.42) : Math.min(n, 12);
    const outerCount = Math.max(0, n - innerCount);

    const cx = 500;
    const cy = 350;

    const rOuter1 = 310;
    const rOuter0 = 250;
    const rInner1 = 220;
    const rInner0 = 155;

    const startAngleOuter = -105;
    const startAngleInner = -95;
    const gapDeg = 1.1;

    const outerSlices =
      outerCount > 0
        ? this.makeRingGeometry({
            count: outerCount,
            startAngle: startAngleOuter,
            gapDeg,
            cx,
            cy,
            r0: rOuter0,
            r1: rOuter1,
          })
        : [];

    const innerSlices =
      innerCount > 0
        ? this.makeRingGeometry({
            count: innerCount,
            startAngle: startAngleInner,
            gapDeg,
            cx,
            cy,
            r0: rInner0,
            r1: rInner1,
          })
        : [];

    const outerKeys = keys.slice(0, outerCount);
    const innerKeys = keys.slice(outerCount);

    const sections: Section[] = [];

    outerKeys.forEach((key, idx) => {
      const list = groups.get(key) ?? [];
      const geo = outerSlices[idx];
      sections.push(
        this.makeSectionFromGroup(key, idx + 1, 'outer', list, geo)
      );
    });

    innerKeys.forEach((key, idx) => {
      const list = groups.get(key) ?? [];
      const geo = innerSlices[idx];
      sections.push(
        this.makeSectionFromGroup(
          key,
          outerCount + idx + 1,
          'inner',
          list,
          geo
        )
      );
    });

    return sections;
  }

  private makeSectionFromGroup(
    key: string,
    number: number,
    ring: 'inner' | 'outer',
    list: Seat[],
    geo: { pathD: string; centroid: { x: number; y: number } }
  ): Section {
    const type = this.getTicketType(key, list);
    const price = this.getSectionPrice(list);
    const available = list.filter(
      (s) => (s.status || '').toLowerCase() === 'available'
    ).length;

    return {
      id: key,
      number,
      label: key,
      ring,
      type,
      price,
      available,
      pathD: geo.pathD,
      centroid: geo.centroid,
      seats: list,
    };
  }

  // ✅ sectionKey from seatNumber prefix: GH-1, GB-2, TG-3, TD-4, VIP-A1...
  private getSectionKey(seat: Seat): string {
    const sn = (seat.seatNumber || '').trim();
    const m = sn.match(/^([A-Za-z]{1,8})\s*[-_]/);
    if (m?.[1]) return m[1].toUpperCase();

    const z = (seat.zone || '').trim();
    if (z) return z.toUpperCase().replace(/\s+/g, '_').slice(0, 12);

    return 'STD';
  }

  private getTicketType(key: string, list: Seat[]): TicketType {
    const k = key.toLowerCase();
    const zone = (list[0]?.zone || '').toLowerCase();
    const sample = `${k} ${zone}`;

    if (sample.includes('vip')) return 'VIP';
    if (
      sample.includes('acc') ||
      sample.includes('pmr') ||
      sample.includes('handi')
    )
      return 'Accessible';
    return 'Standard';
  }

  private getSectionPrice(list: Seat[]): number {
    const prices = list
      .map((s) => s.price)
      .filter((p) => typeof p === 'number' && !Number.isNaN(p));
    if (!prices.length) return 0;
    return Math.min(...prices);
  }

  // -------------------------
  // ✅ Selection: pick REAL seats
  // -------------------------
  onPickSection(s: Section) {
    if (s.available <= 0) return;

    this.selectedSection = s;

    const availableSeats = (s.seats || [])
      .filter((seat) => (seat.status || '').toLowerCase() === 'available')
      .sort((a, b) => a.id - b.id);

    this.selectedSeats = availableSeats.slice(0, this.ticketsCount);

    // ✅ emit REAL seats -> backend will find them
    this.seatsSelected.emit(this.selectedSeats);
  }

  // -------------------------
  // UI helpers
  // -------------------------
  getSectionFillClass(s: Section) {
    if (this.selectedSection?.id === s.id) return 'sec selected';
    if (s.available <= 0) return 'sec soldout';
    if (s.type === 'VIP') return 'sec vip';
    if (s.type === 'Accessible') return 'sec accessible';
    return 'sec standard';
  }

  get filteredSections(): Section[] {
    let list = [...this.sections];

    if (this.filterType !== 'All')
      list = list.filter((s) => s.type === this.filterType);
    list = list.filter((s) => s.available > 0);

    if (this.activeTab === 'Lowest') {
      list.sort((a, b) => a.price - b.price || b.available - a.available);
    } else {
      const ringScore = (x: Section) => (x.ring === 'inner' ? 0 : 1);
      list.sort(
        (a, b) =>
          ringScore(a) - ringScore(b) ||
          b.available - a.available ||
          a.price - b.price
      );
    }

    return list;
  }

  formatType(t: TicketType) {
    if (t === 'VIP') return 'VIP';
    if (t === 'Accessible') return 'Accessible';
    return 'Standard';
  }

  // -------------------------
  // Zoom / Pan
  // -------------------------
  zoomIn() {
    this.setScale(this.scale * 1.15);
  }
  zoomOut() {
    this.setScale(this.scale / 1.15);
  }
  resetView() {
    this.scale = 1;
    this.panX = 0;
    this.panY = 0;
  }

  private setScale(next: number) {
    this.scale = Math.max(0.6, Math.min(3.0, next));
  }

  onWheel(evt: WheelEvent) {
    evt.preventDefault();
    const factor = evt.deltaY > 0 ? 0.92 : 1.08;
    this.setScale(this.scale * factor);
  }

  onPointerDown(evt: PointerEvent) {
    this.isDragging = true;
    this.dragStart = { x: evt.clientX, y: evt.clientY };
    this.panStart = { x: this.panX, y: this.panY };
  }

  onPointerMove(evt: PointerEvent) {
    if (!this.isDragging) return;
    const dx = evt.clientX - this.dragStart.x;
    const dy = evt.clientY - this.dragStart.y;
    this.panX = this.panStart.x + dx;
    this.panY = this.panStart.y + dy;
  }

  onPointerUp() {
    this.isDragging = false;
  }

  get transformG() {
    return `translate(${this.panX} ${this.panY}) scale(${this.scale})`;
  }

  // -------------------------
  // SVG geometry helpers
  // -------------------------
  private polar(cx: number, cy: number, r: number, deg: number) {
    const rad = (deg * Math.PI) / 180;
    return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
  }

  private arcSegmentPath(
    cx: number,
    cy: number,
    r0: number,
    r1: number,
    a0: number,
    a1: number
  ) {
    const p1 = this.polar(cx, cy, r1, a0);
    const p2 = this.polar(cx, cy, r1, a1);
    const p3 = this.polar(cx, cy, r0, a1);
    const p4 = this.polar(cx, cy, r0, a0);

    const largeArc = Math.abs(a1 - a0) > 180 ? 1 : 0;

    return [
      `M ${p1.x.toFixed(2)} ${p1.y.toFixed(2)}`,
      `A ${r1} ${r1} 0 ${largeArc} 1 ${p2.x.toFixed(2)} ${p2.y.toFixed(2)}`,
      `L ${p3.x.toFixed(2)} ${p3.y.toFixed(2)}`,
      `A ${r0} ${r0} 0 ${largeArc} 0 ${p4.x.toFixed(2)} ${p4.y.toFixed(2)}`,
      'Z',
    ].join(' ');
  }

  private segmentCentroid(cx: number, cy: number, rMid: number, aMid: number) {
    return this.polar(cx, cy, rMid, aMid);
  }

  private makeRingGeometry(args: {
    count: number;
    startAngle: number;
    gapDeg: number;
    cx: number;
    cy: number;
    r0: number;
    r1: number;
  }): Array<{ pathD: string; centroid: { x: number; y: number } }> {
    const { count, startAngle, gapDeg, cx, cy, r0, r1 } = args;

    const sweep = 320;
    const step = sweep / Math.max(1, count);

    const slices: Array<{
      pathD: string;
      centroid: { x: number; y: number };
    }> = [];

    for (let i = 0; i < count; i++) {
      const a0 = startAngle + i * step + gapDeg / 2;
      const a1 = startAngle + (i + 1) * step - gapDeg / 2;

      slices.push({
        pathD: this.arcSegmentPath(cx, cy, r0, r1, a0, a1),
        centroid: this.segmentCentroid(cx, cy, (r0 + r1) / 2, (a0 + a1) / 2),
      });
    }
    return slices;
  }
}
