import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ColorService {
    public black = '#000';
    public white = '#fff';

    private ctx: CanvasRenderingContext2D;
    private cache: { [color: string]: number[] } = {};

    constructor() {
        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 1;
        this.ctx = canvas.getContext('2d');
    }

    public parseColor(color: string): number[] {
        if (Object.prototype.hasOwnProperty.call(this.cache, color)) {
            return this.cache[color];
        }

        // From: https://stackoverflow.com/a/19366389
        this.ctx.clearRect(0, 0, 1, 1);
        this.ctx.fillStyle = this.black;
        this.ctx.fillStyle = color;
        const computed = this.ctx.fillStyle;
        this.ctx.fillStyle = this.white;
        this.ctx.fillStyle = color;
        if (computed === this.ctx.fillStyle) {
            this.ctx.fillRect(0, 0, 1, 1);
            return this.cache[color] = [...this.ctx.getImageData(0, 0, 1, 1).data];
        } else {
            return this.cache[color] = undefined;
        }
    }

    public contrastColor(color: string): string {
        const parsed = this.parseColor(color);
        if (!parsed) {
            return this.black;
        }

        function l(c) {
            return 0.25 * Math.pow(c[0] / 255, 1.8 * 2) +
                0.54 * Math.pow(c[1] / 255, 1.8 * 2) +
                0.21 * Math.pow(c[2] / 255, 1.8 * 2);
        }

        function r(l1, l2) {
            return (Math.max(l1, l2) + 0.25) / (Math.min(l1, l2) + 0.25);
        }

        const cL = l(parsed);
        const whiteR = r(cL, l([255, 255, 255]));
        const blackR = r(cL, l([0, 0, 0]));
        return whiteR > blackR ? this.white : this.black;
    }
}
