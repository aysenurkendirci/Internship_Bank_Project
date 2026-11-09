const compact = (iban: string) => iban.replace(/\s+/g, '');

/** The printed form of an IBAN, in groups of four: "TR33 0006 1005 ...". */
export function formatIban(iban: string): string {
  return compact(iban).replace(/(.{4})(?=.)/g, '$1 ');
}

/** ISO 13616 check digits. Catches a mistyped digit before the user reaches the confirmation step. */
export function isValidIban(value: string): boolean {
  const iban = compact(value).toUpperCase();
  if (!/^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$/.test(iban)) return false;

  const digits = (iban.slice(4) + iban.slice(0, 4)).replace(/[A-Z]/g, (letter) => String(letter.charCodeAt(0) - 55));

  let remainder = 0;
  for (const digit of digits) remainder = (remainder * 10 + Number(digit)) % 97;
  return remainder === 1;
}
