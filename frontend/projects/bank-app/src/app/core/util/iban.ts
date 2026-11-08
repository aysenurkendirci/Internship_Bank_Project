/** The printed form of an IBAN, in groups of four: "TR33 0006 1005 ...". */
export function formatIban(iban: string): string {
  return iban.replace(/\s+/g, '').replace(/(.{4})(?=.)/g, '$1 ');
}
