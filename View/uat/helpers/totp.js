import crypto from 'node:crypto'

function decodeBase32(input) {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
  const normalized = String(input || '').toUpperCase().replace(/[^A-Z2-7]/g, '')
  let bits = ''
  for (const character of normalized) {
    const index = alphabet.indexOf(character)
    if (index < 0) throw new Error('Invalid TOTP secret.')
    bits += index.toString(2).padStart(5, '0')
  }
  const bytes = []
  for (let offset = 0; offset + 8 <= bits.length; offset += 8) bytes.push(Number.parseInt(bits.slice(offset, offset + 8), 2))
  return Buffer.from(bytes)
}

export function generateTotp(secret, now = Date.now()) {
  const counter = Buffer.alloc(8)
  counter.writeBigUInt64BE(BigInt(Math.floor(now / 30_000)))
  const digest = crypto.createHmac('sha1', decodeBase32(secret)).update(counter).digest()
  const offset = digest[digest.length - 1] & 0x0f
  const code = (digest.readUInt32BE(offset) & 0x7fffffff) % 1_000_000
  return String(code).padStart(6, '0')
}
