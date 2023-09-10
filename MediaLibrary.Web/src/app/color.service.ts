import { Injectable } from '@angular/core';

export type RGB = readonly [number, number, number];
export type RGBA = readonly [number, number, number, number];

type Colorish = string | RGB | RGBA;
function isRGB(value: Colorish): value is (RGB | RGBA) {
    return Array.isArray(value);
}

@Injectable({
    providedIn: 'root'
})
export class ColorService {
    public readonly black = '#000';
    public readonly white = '#fff';
    public static readonly GAMMA = 1.8;

    private ctx: CanvasRenderingContext2D;
    private cache: { [color: string]: RGBA } = {};

    constructor() {
        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 1;
        this.ctx = canvas.getContext('2d');
    }

    public contrast(a: Colorish, b: Colorish): number {
        return this.contrastRatio(
            this.contrastBrightness(a),
            this.contrastBrightness(b));
    }

    public contrastBrightness(color: Colorish, gamma?: number): number {
        if (!isRGB(color)) {
            color = this.parseColor(color);
        }

        if (typeof gamma === "undefined") {
            gamma = ColorService.GAMMA
        }

        return 0.25 * Math.pow(color[0] / 255, gamma * 2) +
            0.54 * Math.pow(color[1] / 255, gamma * 2) +
            0.21 * Math.pow(color[2] / 255, gamma * 2);
    }

    public contrastColor(color: Colorish): string {
        if (!color) {
            return this.black;
        }

        if (!isRGB(color)) {
            this.parseColor(color);
        }

        const brightness = this.contrastBrightness(color);
        const whiteContrast = this.contrastRatio(brightness, this.contrastBrightness([255, 255, 255]));
        const blackContrast = this.contrastRatio(brightness, this.contrastBrightness([0, 0, 0]));
        return whiteContrast > blackContrast ? this.white : this.black;
    }

    public parseColor(color: string): RGBA {
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
            var d = this.ctx.getImageData(0, 0, 1, 1).data;
            return this.cache[color] = [d[0], d[1], d[2], d[3]];
        } else {
            return this.cache[color] = undefined;
        }
    }

    private contrastRatio(l1: number, l2: number): number {
        return (Math.max(l1, l2) + 0.25) / (Math.min(l1, l2) + 0.25);
    }
}
